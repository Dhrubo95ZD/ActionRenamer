using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ActionRenamer;

/// <summary>Entry point. Wires the text rewriters and the settings window.</summary>
public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/actionrenamer";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IFlyTextGui FlyTextGui { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider Interop { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPartyList Party { get; private set; } = null!;

    public Configuration Config { get; }
    public IconPackInstaller Icons { get; }
    public RenameEngine Engine { get; }
    public DutyAutomation Duty { get; }
    public FollowTank Follow { get; }

    /// <summary>Every distinct status effect name in the game, sorted.</summary>
    public IReadOnlyList<string> StatusNames { get; }

    /// <summary>Name a status shows with the current settings (custom name, else theme, else game name).</summary>
    public string StatusDisplayName(string gameName)
    {
        if (Config.StatusOverrides.TryGetValue(gameName, out var custom))
            return string.IsNullOrWhiteSpace(custom) ? gameName : custom;
        return Config.StatusThemeEnabled ? StatusTheme.Apply(gameName) : gameName;
    }

    /// <summary>Every distinct skill and trait name in the game.</summary>
    public IReadOnlyList<string> SkillNames { get; }

    /// <summary>Theme name for a skill or trait (what shows when you leave "New name" empty).</summary>
    public string SkillThemeName(string gameName) => Config.SkillThemeEnabled ? SkillTheme.Apply(gameName) : gameName;

    private Dictionary<string, string> BuildThemeMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (Config.SkillThemeEnabled)
            JobCoreTheme.AddTo(map, Data);
        if (Config.SkillThemeEnabled)
        {
            foreach (var (from, to) in SkillTheme.JobVariants())
                map.TryAdd(from, to);
            foreach (var name in SkillNames)
            {
                var themed = SkillTheme.Apply(name);
                if (themed != name)
                    map.TryAdd(name, themed);
            }
        }

        foreach (var name in StatusNames)
        {
            // Your own status names win; theme status names only fill gaps the skill theme left.
            var shown = StatusDisplayName(name);
            if (shown == name)
                continue;
            if (Config.StatusOverrides.ContainsKey(name)) map[name] = shown;
            else map.TryAdd(name, shown);
        }
        return map;
    }

    private readonly WindowSystem windows = new("ActionRenamer");
    private readonly ConfigWindow configWindow;
    private readonly TextNodeHook textHook;
    private readonly AddonTextRewriter addonRewriter;
    private readonly TooltipRewriter tooltipRewriter;
    private readonly MessageRewriter messageRewriter;

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        StatusNames = Data.GetExcelSheet<Lumina.Excel.Sheets.Status>()
            .Select(s => s.Name.ExtractText())
            .Where(n => n.Length > 0)
            .Distinct()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        SkillNames = Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().Select(a => a.Name.ExtractText())
            .Concat(Data.GetExcelSheet<Lumina.Excel.Sheets.Trait>().Select(t => t.Name.ExtractText()))
            .Concat(Data.GetExcelSheet<Lumina.Excel.Sheets.CraftAction>().Select(c => c.Name.ExtractText()))
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();
        Engine = new RenameEngine(Config, BuildThemeMap());

        textHook = new TextNodeHook(Interop, Log, Config, () => Engine);
        addonRewriter = new AddonTextRewriter(AddonLifecycle, Log, Config, () => Engine);
        tooltipRewriter = new TooltipRewriter(AddonLifecycle, GameGui, Log, Config);
        messageRewriter = new MessageRewriter(ChatGui, FlyTextGui, Config, () => Engine);

        Icons = new IconPackInstaller(PluginInterface, Framework, ChatGui, Log, Config, () => PluginInterface.SavePluginConfig(Config));

        Duty = new DutyAutomation(AddonLifecycle, Framework, GameGui, DutyState, Condition, Objects, ClientState, Data, ChatGui, Log, Config);
        Follow = new FollowTank(Framework, Condition, Party, Objects, CommandManager, ChatGui, Log, Config);

        configWindow = new ConfigWindow(this, Data);
        windows.AddWindow(configWindow);

        CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Action Renamer settings. \"/actionrenamer stay\" keeps you in the duty you just finished.",
        });

        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.Trim().Equals("stay", StringComparison.OrdinalIgnoreCase))
            Duty.StayInThisDuty();
        else
            configWindow.Toggle();
    }

    /// <summary>Saves settings and applies edits right away. Text already on screen updates when the game redraws it.</summary>
    public void SaveAndApply()
    {
        PluginInterface.SavePluginConfig(Config);
        Engine.Rebuild(Config, BuildThemeMap());
        textHook.ResetCache();
        addonRewriter.ResetCache();
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        CommandManager.RemoveHandler(Command);
        windows.RemoveAllWindows();
        Follow.Dispose();
        Duty.Dispose();
        Icons.Dispose();
        messageRewriter.Dispose();
        tooltipRewriter.Dispose();
        addonRewriter.Dispose();
        textHook.Dispose();
    }
}
