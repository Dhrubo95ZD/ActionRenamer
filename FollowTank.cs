using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ActionRenamer;

/// <summary>A party member as shown in the follow menu.</summary>
public sealed record FollowCandidate(string Name, string Label);

/// <summary>
/// Tells Boss Mod's AI who to follow (/vbmai follow). Automatic: the party's tank in Duty Finder content.
/// A person picked from the menu is followed anywhere while they are in the party.
/// Turning following off keeps Boss Mod's dodging but stops it walking after anyone.
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
    private bool stoppedSent = true;

    /// <summary>A party member picked by hand, or null for automatic.</summary>
    public string? ChosenName { get; private set; }

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
    public string Status => following != null ? $"Following {Initials(following)}." : config.AutoFollowTank ? "Not following anyone right now." : "Following is off.";

    /// <summary>Short header label: who is followed, by initials.</summary>
    public string ChipText => !config.AutoFollowTank ? "Follow off" : following != null ? $"Follow {Initials(following)}" : ChosenName != null ? "Follow (waiting)" : "Follow tank";

    /// <summary>Follow this party member, or null to go back to following the tank automatically.</summary>
    public void Choose(string? name)
    {
        ChosenName = name;
        following = null;
        nextCheck = DateTime.MinValue;
    }

    /// <summary>Everyone else in the party, labelled by initials and role, e.g. "A.D (Tank)".</summary>
    public IReadOnlyList<FollowCandidate> Candidates()
    {
        var me = objects.LocalPlayer;
        var list = new List<FollowCandidate>();
        foreach (var m in party)
        {
            if (me != null && m.EntityId == me.EntityId)
                continue;
            var name = m.Name.TextValue;
            if (name.Length == 0)
                continue;
            list.Add(new FollowCandidate(name, $"{Initials(name)} ({RoleName(m.ClassJob.ValueNullable?.Role ?? 0)})"));
        }
        return list;
    }

    /// <summary>"Ash Dhru" becomes "A.D".</summary>
    public static string Initials(string name) =>
        string.Join('.', name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpperInvariant(w[0])));

    private static string RoleName(byte role) => role switch
    {
        TankRole => "Tank",
        HealerRole => "Healer",
        2 or 3 => "DPS",
        _ => "?",
    };

    private void OnUpdate(IFramework _)
    {
        var now = DateTime.UtcNow;
        if (now < nextCheck)
            return;
        nextCheck = now.AddSeconds(2);

        try
        {
            if (!config.AutoFollowTank)
            {
                StopFollowing();
                return;
            }

            var target = PickTarget();
            if (target == null)
            {
                if (following != null)
                    StopFollowing();
                return;
            }
            if (target == following)
                return;

            var distance = Math.Clamp(config.FollowDistance, 3, 25);
            commands.ProcessCommand($"/vbmai DistanceToMaster {distance}");
            if (!commands.ProcessCommand($"/vbmai follow {target}"))
            {
                chat.Print("[Action Renamer] Boss Mod is not loaded, so following is off.");
                following = target; // do not retry every two seconds
                return;
            }
            following = target;
            stoppedSent = false;
            chat.Print($"[Action Renamer] Boss Mod is following {Initials(target)}.");
        }
        catch (Exception e)
        {
            log.Error(e, "Follow failed");
            nextCheck = now.AddSeconds(30);
        }
    }

    /// <summary>The chosen person while they are in the party; otherwise the tank (or healer) in Duty Finder content.</summary>
    private string? PickTarget()
    {
        var me = objects.LocalPlayer;
        if (me == null || party.Length < 2)
            return null;

        if (ChosenName != null)
        {
            if (party.Any(m => m.EntityId != me.EntityId && m.Name.TextValue == ChosenName))
                return ChosenName;
            ChosenName = null; // they left: back to automatic
            chat.Print("[Action Renamer] The person you chose left the party. Back to following the tank.");
        }

        if (!InDutyFinderContent())
        {
            toldNoTank = false;
            return null;
        }

        var myRole = me.ClassJob.ValueNullable?.Role ?? 0;
        if (myRole == TankRole)
            return null; // the tank leads

        string? Find(byte role) => party
            .Where(m => m.EntityId != me.EntityId && m.ClassJob.ValueNullable?.Role == role)
            .Select(m => m.Name.TextValue)
            .FirstOrDefault(n => n.Length > 0);

        var pick = Find(TankRole) ?? (myRole == HealerRole ? null : Find(HealerRole));
        if (pick == null && !toldNoTank)
        {
            chat.Print("[Action Renamer] No tank in the party to follow.");
            toldNoTank = true;
        }
        return pick;
    }

    /// <summary>Boss Mod has no "unfollow": following party slot 1 (yourself) keeps its AI dodging but stops the walking.</summary>
    private void StopFollowing()
    {
        following = null;
        if (stoppedSent)
            return;
        commands.ProcessCommand("/vbmai follow slot1");
        stoppedSent = true;
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
