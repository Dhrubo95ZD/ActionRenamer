using System;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ActionRenamer;

/// <summary>
/// Queues for one chosen duty through the Duty Finder, again and again, until the chosen number of runs is done.
/// Each run counts once you are back outside the duty. Commence and leaving are done by <see cref="DutyAutomation"/>.
/// </summary>
public sealed unsafe class AutoQueue : IDisposable
{
    private enum Step { OpenFinder, WaitForQueue }

    private const int MaxQueueAttempts = 3;

    private readonly IFramework framework;
    private readonly ICondition condition;
    private readonly IGameGui gameGui;
    private readonly IPartyList party;
    private readonly IObjectTable objects;
    private readonly IChatGui chat;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly DutyCatalog catalog;

    private Step step;
    private DateTime nextAction;
    private DateTime stepStarted;
    private int attempts;
    private bool wasInDuty;

    public bool Running { get; private set; }
    public int RunsDone { get; private set; }
    public int RunsWanted { get; private set; }
    public DutyEntry? Duty { get; private set; }
    public string Status { get; private set; } = "Stopped.";

    public AutoQueue(IFramework framework, ICondition condition, IGameGui gameGui, IPartyList party, IObjectTable objects,
        IChatGui chat, IPluginLog log, Configuration config, DutyCatalog catalog)
    {
        this.framework = framework;
        this.condition = condition;
        this.gameGui = gameGui;
        this.party = party;
        this.objects = objects;
        this.chat = chat;
        this.log = log;
        this.config = config;
        this.catalog = catalog;
        framework.Update += OnUpdate;
    }

    /// <summary>Starts queueing for the duty chosen in the settings.</summary>
    public void Start()
    {
        if (!catalog.ById.TryGetValue(config.QueueDutyId, out var duty))
        {
            Status = "Choose a duty first.";
            return;
        }
        Duty = duty;
        RunsWanted = Math.Clamp(config.QueueRuns, 1, 999);
        RunsDone = 0;
        Running = true;
        wasInDuty = InDuty;
        attempts = 0;
        BeginQueue();
        chat.Print($"[Action Renamer] Auto-queue: {duty.Name}, {RunsWanted} run(s).");
    }

    public void Stop(string why = "Stopped.")
    {
        if (Running && Duty != null)
            chat.Print($"[Action Renamer] Auto-queue stopped after {RunsDone} of {RunsWanted} run(s) of {Duty.Name}.");
        Running = false;
        Status = why;
    }

    private void BeginQueue()
    {
        step = Step.OpenFinder;
        stepStarted = DateTime.UtcNow;
        nextAction = DateTime.UtcNow.AddSeconds(1);
    }

    private bool InDuty => condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95];

    private bool InQueue => condition[ConditionFlag.InDutyQueue] || condition[ConditionFlag.WaitingForDuty] || condition[ConditionFlag.WaitingForDutyFinder];

    private bool Busy => condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51] || condition[ConditionFlag.InCombat]
        || condition[ConditionFlag.Casting] || condition[ConditionFlag.OccupiedInCutSceneEvent] || condition[ConditionFlag.WatchingCutscene]
        || condition[ConditionFlag.Occupied] || condition[ConditionFlag.OccupiedInQuestEvent];

    private void OnUpdate(IFramework _)
    {
        if (!Running)
            return;
        try
        {
            Tick(DateTime.UtcNow);
        }
        catch (Exception e)
        {
            log.Error(e, "Auto-queue failed");
            Stop("Stopped after an error (see /xllog).");
        }
    }

    private void Tick(DateTime now)
    {
        if (InDuty)
        {
            wasInDuty = true;
            Status = $"In {Duty!.Name}: run {RunsDone + 1} of {RunsWanted}.";
            return;
        }
        if (condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51])
            return;

        if (wasInDuty)
        {
            wasInDuty = false;
            RunsDone++;
            if (RunsDone >= RunsWanted)
            {
                chat.Print($"[Action Renamer] Auto-queue finished: {RunsDone} run(s) of {Duty!.Name}.");
                Running = false;
                Status = $"Finished {RunsDone} run(s).";
                return;
            }
            attempts = 0;
            BeginQueue();
            nextAction = now.AddSeconds(Math.Clamp(config.QueueDelaySeconds, 2, 120));
        }

        if (InQueue || IsVisible("ContentsFinderConfirm"))
        {
            attempts = 0;
            step = Step.WaitForQueue;
            Status = $"In the queue for {Duty!.Name} (run {RunsDone + 1} of {RunsWanted}).";
            return;
        }

        if (now < nextAction)
            return;
        if (Busy)
        {
            Status = "Waiting until you are free to queue.";
            nextAction = now.AddSeconds(1);
            return;
        }
        if (!CanQueueAsLeader(out var why))
        {
            Status = why;
            nextAction = now.AddSeconds(3);
            return;
        }
        var agent = AgentContentsFinder.Instance();
        if (agent != null && agent->DutyPenaltyMinutes > 0)
        {
            Status = $"Waiting out a {agent->DutyPenaltyMinutes} minute withdrawal penalty.";
            nextAction = now.AddSeconds(10);
            return;
        }

        Queue(now);
    }

    /// <summary>The Duty Finder steps: open it on the duty, make it the only ticked duty, press Join.</summary>
    private void Queue(DateTime now)
    {
        var duty = Duty!;
        var agent = AgentContentsFinder.Instance();
        var finder = ContentsFinder.Instance();
        if (agent == null || finder == null)
            return;
        nextAction = now.AddMilliseconds(600);

        if (step == Step.WaitForQueue)
        {
            // Join was pressed but no queue started.
            if (now - stepStarted < TimeSpan.FromSeconds(8))
                return;
            attempts++;
            if (attempts >= MaxQueueAttempts)
            {
                Stop($"Could not queue for {duty.Name}. Check that it is unlocked and your party fits it.");
                return;
            }
            BeginQueue();
            return;
        }

        if (finder->IsUnrestrictedParty != config.QueueUnsynced)
        {
            finder->IsUnrestrictedParty = config.QueueUnsynced;
            return;
        }

        // Open the Duty Finder with the duty highlighted.
        var addon = (AddonContentsFinder*)gameGui.GetAddonByName("ContentsFinder").Address;
        var ready = addon != null && ((AtkUnitBase*)addon)->IsVisible && ((AtkUnitBase*)addon)->IsReady;
        if (!ready || agent->SelectedDuty.Id != duty.CfcId)
        {
            Status = $"Opening the Duty Finder on {duty.Name}...";
            agent->OpenRegularDuty(duty.CfcId, false);
            nextAction = now.AddMilliseconds(1200);
            return;
        }
        if (addon->DutyList == null || addon->DutyList->Items.LongCount == 0)
            return;

        // Make the ticked duties exactly this one, reading what the window says is ticked, then join.
        var unit = (AtkUnitBase*)addon;
        var ticked = TickedDutyName(unit);
        if (ticked.Length > 0 && ticked != Normalise(duty.Name))
        {
            Fire(unit, 12, 1); // "Clear Selection"
            CountSelectStep(duty);
            return;
        }
        if (ticked.Length == 0)
        {
            SelectHighlighted(addon);
            CountSelectStep(duty);
            return;
        }

        Status = $"Joining the queue for {duty.Name}...";
        Fire(unit, 12, 0); // "Join"
        step = Step.WaitForQueue;
        stepStarted = now;
        selectSteps = 0;
    }

    private int selectSteps;

    /// <summary>Gives up when ticking the duty keeps failing, so the plugin never clicks forever.</summary>
    private void CountSelectStep(DutyEntry duty)
    {
        if (++selectSteps < 12)
            return;
        selectSteps = 0;
        Stop($"Could not select {duty.Name} in the Duty Finder.");
    }

    /// <summary>The duty the Duty Finder shows as ticked (value 18), as letters and digits only. Empty when none.</summary>
    private static string TickedDutyName(AtkUnitBase* addon)
    {
        if (addon->AtkValues == null || addon->AtkValuesCount <= 18)
            return string.Empty;
        return Normalise(addon->AtkValues[18].GetValueAsString());
    }

    private static string Normalise(string text) =>
        new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    /// <summary>Ticks the duty the Duty Finder opened on. The callback counts list rows without the section headers.</summary>
    private static void SelectHighlighted(AddonContentsFinder* addon)
    {
        var list = addon->DutyList;
        if (list == null)
            return;
        var index = list->SelectedItemIndex;
        uint headers = 0;
        var items = list->Items;
        for (var i = 0; i < index && i < (int)items.LongCount; i++)
        {
            var item = items[i].Value;
            if (item != null && item->UIntValues.LongCount > 0 && item->UIntValues[0] is 0 or 1)
                headers++;
        }
        Fire((AtkUnitBase*)addon, 3, (int)(headers + 1));
    }

    private bool CanQueueAsLeader(out string why)
    {
        why = string.Empty;
        if (party.Length <= 1)
            return true;
        var me = objects.LocalPlayer;
        var leaderIndex = (int)party.PartyLeaderIndex;
        if (me != null && leaderIndex >= 0 && leaderIndex < party.Length && party[leaderIndex]?.EntityId == me.EntityId)
            return true;
        why = "Only the party leader can queue. Waiting.";
        return false;
    }

    private bool IsVisible(string addonName)
    {
        var addon = (AtkUnitBase*)gameGui.GetAddonByName(addonName).Address;
        return addon != null && addon->IsVisible;
    }

    private static void Fire(AtkUnitBase* addon, params int[] values)
    {
        var atk = stackalloc AtkValue[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            atk[i].Type = AtkValueType.Int;
            atk[i].Int = values[i];
        }
        addon->FireCallback((uint)values.Length, atk, true);
    }

    public void Dispose() => framework.Update -= OnUpdate;
}
