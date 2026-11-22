using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ActionRenamer;

/// <summary>
/// Stops Boss Mod's AI from pulling enemies by itself in unsynced duties.
/// Its "VBM Multibox" preset (the one "/vbmai" and following use) has "Prioritize all targets in unsynced duties"
/// switched on: unsynced, it targets and auto-attacks enemies nobody has pulled yet, bosses included.
/// This sets that one option off through Boss Mod's own plugin interface. The change lives only in memory,
/// so it is re-applied every few seconds (cheap, and covers Boss Mod reloading).
/// </summary>
public sealed class BossModGuard : IDisposable
{
    private const string Preset = "VBM Multibox";
    private const string Module = "BossMod.Autorotation.MiscAI.AutoTarget";
    private const string Track = "EE";

    private readonly IDalamudPluginInterface pi;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private DateTime nextApply;
    private bool applied;

    public BossModGuard(IDalamudPluginInterface pi, IFramework framework, IPluginLog log, Configuration config)
    {
        this.pi = pi;
        this.framework = framework;
        this.log = log;
        this.config = config;
        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        var now = DateTime.UtcNow;
        if (now < nextApply)
            return;
        nextApply = now.AddSeconds(10);
        try
        {
            if (config.NoUnsyncedPulls)
            {
                applied = pi.GetIpcSubscriber<string, string, string, string, bool>("BossMod.Presets.AddTransientStrategy")
                    .InvokeFunc(Preset, Module, Track, "Disabled");
            }
            else if (applied)
            {
                pi.GetIpcSubscriber<string, string, string, bool>("BossMod.Presets.ClearTransientStrategy")
                    .InvokeFunc(Preset, Module, Track);
                applied = false;
            }
        }
        catch (Exception e)
        {
            // Boss Mod not installed or not loaded yet; try again later.
            applied = false;
            log.Verbose($"Boss Mod guard: {e.Message}");
            nextApply = now.AddSeconds(30);
        }
    }

    public void Dispose()
    {
        framework.Update -= OnUpdate;
        if (!applied)
            return;
        try
        {
            pi.GetIpcSubscriber<string, string, string, bool>("BossMod.Presets.ClearTransientStrategy").InvokeFunc(Preset, Module, Track);
        }
        catch
        {
            // Boss Mod already gone.
        }
    }
}
