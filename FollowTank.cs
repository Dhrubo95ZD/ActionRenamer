using System;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ActionRenamer;

/// <summary>
/// In Duty Finder content, tells Boss Mod's AI to follow the party's tank (/vbmai follow).
/// Boss Mod then walks after the tank between fights and dodges on its own during boss fights.
/// Picks the tank again when the party changes; does nothing when you are the tank or there is none.
/// </summary>
public sealed unsafe class FollowTank : IDisposable
{
    private const byte TankRole = 1;
    private const byte HealerRole = 4;

    private readonly IFramework framework;
    private readonly ICondition condition;
    private readonly IPartyList party;
    private readonly IObjectTable objects;
    private readonly ICommandManager commands;
    private readonly IChatGui chat;
    private readonly IPluginLog log;
    private readonly Configuration config;

    private DateTime nextCheck;
    private string? following;
    private bool toldNoTank;

    public FollowTank(IFramework framework, ICondition condition, IPartyList party, IObjectTable objects,
        ICommandManager commands, IChatGui chat, IPluginLog log, Configuration config)
    {
        this.framework = framework;
        this.condition = condition;
        this.party = party;
        this.objects = objects;
        this.commands = commands;
        this.chat = chat;
        this.log = log;
        this.config = config;
        framework.Update += OnUpdate;
    }

    /// <summary>Who Boss Mod is following right now, for the settings window.</summary>
    public string Status => following != null ? $"Following {following}." : "Not following anyone.";

    private void OnUpdate(IFramework _)
    {
        var now = DateTime.UtcNow;
        if (now < nextCheck)
            return;
        nextCheck = now.AddSeconds(2);

        try
        {
            if (!config.AutoFollowTank || !InDutyFinderContent())
            {
                following = null;
                toldNoTank = false;
                return;
            }

            var me = objects.LocalPlayer;
            if (me == null || party.Length < 2)
                return;

            // The tank leads; a tank never follows.
            var myRole = me.ClassJob.ValueNullable?.Role ?? 0;
            if (myRole == TankRole)
            {
                following = null;
                return;
            }

            var tank = party
                .Where(m => m.EntityId != me.EntityId && m.ClassJob.ValueNullable?.Role == TankRole)
                .Select(m => m.Name.TextValue)
                .FirstOrDefault(n => n.Length > 0);
            if (tank == null)
            {
                // No tank in the party (for example a healer-led run): follow the healer if you are not one.
                tank = myRole == HealerRole ? null : party
                    .Where(m => m.EntityId != me.EntityId && m.ClassJob.ValueNullable?.Role == HealerRole)
                    .Select(m => m.Name.TextValue)
                    .FirstOrDefault(n => n.Length > 0);
            }
            if (tank == null)
            {
                if (!toldNoTank)
                    chat.Print("[Action Renamer] No tank in the party to follow.");
                toldNoTank = true;
                following = null;
                return;
            }
            if (tank == following)
                return;

            var distance = Math.Clamp(config.FollowDistance, 3, 25);
            commands.ProcessCommand($"/vbmai DistanceToMaster {distance}");
            if (!commands.ProcessCommand($"/vbmai follow {tank}"))
            {
                chat.Print("[Action Renamer] Boss Mod is not loaded, so following the tank is off.");
                following = tank; // do not retry every two seconds
                return;
            }
            following = tank;
            chat.Print($"[Action Renamer] Boss Mod is following {tank}.");
        }
        catch (Exception e)
        {
            log.Error(e, "Follow tank failed");
            nextCheck = now.AddSeconds(30);
        }
    }

    private bool InDutyFinderContent()
    {
        if (!(condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95]))
            return false;
        var game = GameMain.Instance();
        return game != null && game->CurrentContentFinderConditionId != 0;
    }

    public void Dispose() => framework.Update -= OnUpdate;
}
