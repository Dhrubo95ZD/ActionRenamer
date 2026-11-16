using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.DutyState;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ActionRenamer;

/// <summary>
/// Duty helpers: presses Commence when a duty pops, answers Yes to ready checks, and leaves a finished
/// duty once every treasure coffer is open and the loot rolls are over.
/// </summary>
public sealed unsafe class DutyAutomation : IDisposable
{
    /// <summary>Addon sheet row of the ready check prompt ("A ready check has been commenced. ...").</summary>
    private const uint ReadyCheckPromptRow = 8133;

    /// <summary>Addon sheet row of "Move immediately to sealed area?", asked when a boss arena seals with you outside it.</summary>
    private const uint SealedAreaPromptRow = 102631;

    /// <summary>Wait this long after a window opens before pressing its button, so the window is fully set up.</summary>
    private static readonly TimeSpan ClickDelay = TimeSpan.FromMilliseconds(600);

    /// <summary>The game hands loot out by itself five minutes after a coffer is opened; never wait longer than that.</summary>
    private static readonly TimeSpan LootTimeout = TimeSpan.FromSeconds(315);

    /// <summary>Give the coffer this long to appear after the duty completes before deciding there is none.</summary>
    private static readonly TimeSpan CofferSpawnTime = TimeSpan.FromSeconds(10);

    /// <summary>Stop waiting for coffers nobody opens (another alliance's coffer, one left behind) after this long.</summary>
    private static readonly TimeSpan CofferTimeout = TimeSpan.FromSeconds(120);

    private const uint NoObject = 0xE0000000;
    private const int MaxLeaveAttempts = 3;

    private readonly IAddonLifecycle lifecycle;
    private readonly IFramework framework;
    private readonly IGameGui gameGui;
    private readonly IDutyState dutyState;
    private readonly ICondition condition;
    private readonly IObjectTable objects;
    private readonly IClientState clientState;
    private readonly IChatGui chat;
    private readonly IPluginLog log;
    private readonly Configuration config;

    /// <summary>First line of the ready check prompt in the game's language, without whitespace.</summary>
    private readonly string readyCheckPrompt;

    /// <summary>The sealed area question in the game's language, without whitespace.</summary>
    private readonly string sealedAreaPrompt;

    private DateTime? commenceAt;
    private DateTime? readyYesAt;
    private nint readyYesAddon;

    // Auto-leave state, all cleared when the zone changes.
    private bool dutyDone;
    private DateTime doneAt;
    private bool stayThisDuty;
    private DateTime? clearSince;
    private DateTime? lootSeenAt;
    private DateTime nextLeaveTry;
    private DateTime? refusedSince;
    private int leaveAttempts;
    private DateTime nextCheck;

    /// <summary>What the auto-leave is doing right now, for the settings window.</summary>
    public string Status { get; private set; } = "Not in a finished duty.";

    public DutyAutomation(
        IAddonLifecycle lifecycle, IFramework framework, IGameGui gameGui, IDutyState dutyState, ICondition condition,
        IObjectTable objects, IClientState clientState, IDataManager data, IChatGui chat, IPluginLog log, Configuration config)
    {
        this.lifecycle = lifecycle;
        this.framework = framework;
        this.gameGui = gameGui;
        this.dutyState = dutyState;
        this.condition = condition;
        this.objects = objects;
        this.clientState = clientState;
        this.chat = chat;
        this.log = log;
        this.config = config;

        readyCheckPrompt = FirstLine(data, ReadyCheckPromptRow);
        sealedAreaPrompt = FirstLine(data, SealedAreaPromptRow);

        lifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinderConfirm", OnDutyPop);
        lifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", OnYesNo);
        dutyState.DutyCompleted += OnDutyCompleted;
        clientState.TerritoryChanged += OnTerritoryChanged;
        framework.Update += OnUpdate;
    }

    /// <summary>Stops the auto-leave for the duty you are in now (the next duty leaves again).</summary>
    public void StayInThisDuty()
    {
        if (!dutyDone && !InDuty)
        {
            chat.Print("[Action Renamer] You are not in a duty.");
            return;
        }
        stayThisDuty = true;
        Status = "Auto-leave is off for this duty.";
        chat.Print("[Action Renamer] Staying in this duty. Auto-leave is back on for the next one.");
    }

    private void OnDutyPop(AddonEvent type, AddonArgs args)
    {
        if (config.AutoCommenceDuty)
            commenceAt = DateTime.UtcNow + ClickDelay;
    }

    private void OnYesNo(AddonEvent type, AddonArgs args)
    {
        if (!config.AutoReadyCheck && !config.AutoSealedArea)
            return;
        try
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null)
                return;
            // The values the window was opened with are the surest source; the window's own copy is the fallback.
            var prompt = args is AddonSetupArgs { AtkValues: not 0, AtkValueCount: > 0 } setup
                ? FirstString(new Span<AtkValue>((void*)setup.AtkValues, (int)setup.AtkValueCount))
                : string.Empty;
            if (prompt.Length == 0)
                prompt = PromptText(addon);
            if (!ShouldAnswerYes(prompt))
                return;
            readyYesAddon = (nint)addon;
            readyYesAt = DateTime.UtcNow + ClickDelay;
        }
        catch (Exception e)
        {
            log.Error(e, "Yes/No detection failed");
        }
    }

    /// <summary>First non-empty line of an Addon sheet row, without whitespace.</summary>
    private static string FirstLine(IDataManager data, uint row)
    {
        var text = data.GetExcelSheet<Lumina.Excel.Sheets.Addon>().GetRowOrDefault(row)?.Text.ExtractText() ?? string.Empty;
        return Squash(text.Split('\n', '\r').FirstOrDefault(l => l.Trim().Length > 0) ?? string.Empty);
    }

    /// <summary>True for the Yes/No questions this plugin answers by itself, as switched on in the settings.</summary>
    private bool ShouldAnswerYes(string prompt)
    {
        var text = Squash(prompt);
        if (config.AutoReadyCheck && readyCheckPrompt.Length >= 6 && text.Contains(readyCheckPrompt, StringComparison.Ordinal))
            return true;
        return config.AutoSealedArea && sealedAreaPrompt.Length >= 6 && text.Contains(sealedAreaPrompt, StringComparison.Ordinal);
    }

    /// <summary>The question a Yes/No window shows: the value it was opened with, else the text on screen.</summary>
    private static string PromptText(AtkUnitBase* addon)
    {
        if (addon->AtkValues != null && addon->AtkValuesCount > 0)
        {
            var text = FirstString(new Span<AtkValue>(addon->AtkValues, addon->AtkValuesCount));
            if (text.Length > 0)
                return text;
        }
        var node = ((FFXIVClientStructs.FFXIV.Client.UI.AddonSelectYesno*)addon)->PromptText;
        return node == null ? string.Empty : MemoryHelper.ReadSeStringNullTerminated((nint)node->NodeText.StringPtr.Value).TextValue;
    }

    private static string FirstString(Span<AtkValue> values)
    {
        foreach (ref var value in values)
        {
            var kind = value.Type & AtkValueType.TypeMask;
            if (kind is (AtkValueType.String or AtkValueType.ConstString) && value.String.HasValue)
                return MemoryHelper.ReadSeStringNullTerminated((nint)value.String.Value).TextValue;
        }
        return string.Empty;
    }

    private static string Squash(string text) => new(text.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private void OnDutyCompleted(IDutyStateEventArgs args)
    {
        // Duty Finder content only; solo quest battles end by themselves.
        var game = GameMain.Instance();
        if (game == null || game->CurrentContentFinderConditionId == 0)
            return;
        dutyDone = true;
        doneAt = DateTime.UtcNow;
        clearSince = null;
        lootSeenAt = null;
        leaveAttempts = 0;
        nextLeaveTry = DateTime.MinValue;
        refusedSince = null;
    }

    private void OnTerritoryChanged(uint territory) => ResetLeave();

    private bool InDuty => condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56]
        || condition[ConditionFlag.BoundByDuty95];

    private void ResetLeave()
    {
        dutyDone = false;
        refusedSince = null;
        stayThisDuty = false;
        clearSince = null;
        lootSeenAt = null;
        leaveAttempts = 0;
        Status = "Not in a finished duty.";
    }

    private void OnUpdate(IFramework _)
    {
        try
        {
            var now = DateTime.UtcNow;
            PressPending(now);

            if (now < nextCheck)
                return;
            nextCheck = now.AddMilliseconds(250);
            UpdateLeave(now);
        }
        catch (Exception e)
        {
            log.Error(e, "Duty automation failed");
            commenceAt = null;
            readyYesAt = null;
            dutyDone = false;
        }
    }

    private void PressPending(DateTime now)
    {
        if (commenceAt is { } commence && now >= commence)
        {
            commenceAt = null;
            var addon = (AtkUnitBase*)gameGui.GetAddonByName("ContentsFinderConfirm").Address;
            if (config.AutoCommenceDuty && addon != null && addon->IsVisible)
                Fire(addon, 8); // 8 = the Commence button
        }

        if (readyYesAt is { } ready && now >= ready)
        {
            readyYesAt = null;
            var addon = (AtkUnitBase*)gameGui.GetAddonByName("SelectYesno").Address;
            // Only answer the same window that asked; another Yes/No may have replaced it by now.
            if (addon != null && (nint)addon == readyYesAddon && addon->IsVisible && ShouldAnswerYes(PromptText(addon)))
                Fire(addon, 0); // 0 = Yes
        }
    }

    private static void Fire(AtkUnitBase* addon, int choice)
    {
        var value = new AtkValue { Type = AtkValueType.Int, Int = choice };
        addon->FireCallback(1, &value, true);
    }

    private void UpdateLeave(DateTime now)
    {
        if (!dutyDone)
            return;
        if (!InDuty)
        {
            ResetLeave();
            return;
        }
        if (!config.AutoLeaveDuty || stayThisDuty)
        {
            clearSince = null;
            Status = stayThisDuty ? "Auto-leave is off for this duty." : "Auto-leave is switched off.";
            return;
        }

        var hold = HoldReason(now);
        if (hold != null)
        {
            clearSince = null;
            Status = "Waiting: " + hold;
            return;
        }

        clearSince ??= now;
        var left = TimeSpan.FromSeconds(Math.Clamp(config.AutoLeaveDelaySeconds, 1, 120)) - (now - clearSince.Value);
        if (left > TimeSpan.Zero)
        {
            Status = $"Loot is done. Leaving in {Math.Ceiling(left.TotalSeconds):0}s.";
            return;
        }

        if (now < nextLeaveTry)
            return;
        if (leaveAttempts >= MaxLeaveAttempts)
        {
            Status = "Could not leave the duty. Leave by hand.";
            return;
        }
        // The game can refuse for a moment (a transition, a result screen). If it keeps refusing, try anyway.
        if (!EventFramework.CanLeaveCurrentContent())
        {
            refusedSince ??= now;
            if (now - refusedSince.Value < TimeSpan.FromSeconds(15))
            {
                Status = "Waiting: the game does not allow leaving yet.";
                return;
            }
        }
        refusedSince = null;

        leaveAttempts++;
        nextLeaveTry = now.AddSeconds(8);
        Status = "Leaving the duty...";
        if (leaveAttempts == 1)
            chat.Print("[Action Renamer] Loot is done. Leaving the duty.");
        EventFramework.LeaveCurrentContent(false);
    }

    /// <summary>Why the duty cannot be left yet, or null when nothing is in the way.</summary>
    private string? HoldReason(DateTime now)
    {
        if (condition[ConditionFlag.InCombat])
            return "in combat.";
        if (condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51]
            || condition[ConditionFlag.WatchingCutscene] || condition[ConditionFlag.WatchingCutscene78]
            || condition[ConditionFlag.OccupiedInCutSceneEvent])
            return "cutscene or loading.";

        var sinceDone = now - doneAt;
        if (sinceDone < CofferSpawnTime)
            return "giving the treasure coffer time to appear.";

        if (sinceDone < CofferTimeout)
        {
            foreach (var obj in objects)
            {
                if (obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure || !obj.IsTargetable)
                    continue;
                var coffer = (Treasure*)obj.Address;
                if ((coffer->Flags & (Treasure.TreasureFlags.Opened | Treasure.TreasureFlags.FadedOut)) == 0)
                    return $"a treasure coffer is not opened yet ({Math.Ceiling((CofferTimeout - sinceDone).TotalSeconds):0}s left).";
            }
        }

        var pending = PendingLoot();
        if (pending == 0)
        {
            lootSeenAt = null;
            return null;
        }
        lootSeenAt ??= now;
        if (now - lootSeenAt.Value > LootTimeout)
            return null;
        return config.AutoLeaveAfterOwnRolls
            ? $"{pending} loot item(s) you have not rolled on."
            : $"{pending} loot item(s) still being rolled for.";
    }

    /// <summary>Loot items that still keep us here.</summary>
    private int PendingLoot()
    {
        var loot = Loot.Instance();
        if (loot == null)
            return 0;

        var count = 0;
        foreach (ref readonly var item in loot->Items)
        {
            if (item.ItemId == 0 || item.ChestObjectId is 0 or NoObject)
                continue;
            if (item.RollResult is RollResult.Awarded or RollResult.Unknown)
                continue;

            if (config.AutoLeaveAfterOwnRolls)
            {
                // Only items we can still roll on count; a roll that is in stays valid after leaving.
                if (item.RollResult == RollResult.UnAwarded
                    && item.RollState is RollState.UpToNeed or RollState.UpToGreed or RollState.UpToPass)
                    count++;
            }
            else
            {
                count++;
            }
        }
        return count;
    }

    public void Dispose()
    {
        framework.Update -= OnUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;
        dutyState.DutyCompleted -= OnDutyCompleted;
        lifecycle.UnregisterListener(OnDutyPop, OnYesNo);
    }
}
