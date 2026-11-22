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
    [PluginService] internal static IDtrBar DtrBar { get; private set; } = null!;

    /// <summary>"Ask Claude" in the status bar at the bottom right; a click opens the chat.</summary>
    private Dalamud.Game.Gui.Dtr.IDtrBarEntry? askEntry;

    public Configuration Config { get; }
    public IconPackInstaller Icons { get; }
    public RenameEngine Engine { get; }
    public DutyAutomation Duty { get; }
    public FollowTank Follow { get; }
    public DutyCatalog Duties { get; }
    public AutoQueue Queue { get; }

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
        Duties = new DutyCatalog(Data);
        bossModGuard = new BossModGuard(PluginInterface, Framework, Log, Config);
        Queue = new AutoQueue(Framework, Condition, GameGui, Party, Objects, ChatGui, Log, Config, Duties);
        Knowledge = new GameKnowledge(Data, Duties);
        Ask = new AskClaude(Config, Knowledge, Log, PluginInterface.ConfigDirectory.FullName);

        configWindow = new ConfigWindow(this, Data);
        windows.AddWindow(configWindow);
        AskWindow = new AskWindow(this);
        windows.AddWindow(AskWindow);

        CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Action Renamer settings. \"/actionrenamer stay\" keeps you in the duty you just finished.",
        });
        // "/ask" may belong to another plugin; "/askclaude" always works.
        askCommands.Add(AskCommand);
        CommandManager.AddHandler(AskCommand, new CommandInfo(OnAsk) { HelpMessage = "Ask Claude about the game, e.g. /askclaude where does the Ifrit quest start" });
        if (CommandManager.AddHandler("/ask", new CommandInfo(OnAsk) { HelpMessage = "Ask Claude about the game (same as /askclaude)." }))
            askCommands.Add("/ask");

        try
        {
            askEntry = DtrBar.Get("Action Renamer: Ask Claude");
            askEntry.Text = "Ask Claude";
            askEntry.Tooltip = "Open the Ask Claude chat";
            askEntry.OnClick = _ => AskWindow.Toggle();
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not add the Ask Claude status bar button");
        }

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    private void DrawUi()
    {
        if (askEntry != null)
            askEntry.Shown = Config.AskEnabled && Config.AskStatusBar;
        windows.Draw();
    }

    private readonly BossModGuard bossModGuard;

    private const string AskCommand = "/askclaude";
    private readonly List<string> askCommands = new();

    public GameKnowledge Knowledge { get; }
    public AskClaude Ask { get; }
    public AskWindow AskWindow { get; }

    private void OnAsk(string command, string arguments)
    {
        if (!Config.AskEnabled)
        {
            ChatGui.Print("[Action Renamer] Ask Claude is switched off in the settings (General page).");
            return;
        }
        AskWindow.AskNow(arguments);
    }

    /// <summary>What Claude is told about you: job, level, zone and duty. Read on the game thread.</summary>
    public string PlayerContext()
    {
        var me = Objects.LocalPlayer;
        if (me == null)
            return "not logged in";
        var job = me.ClassJob.ValueNullable?.Name.ExtractText() ?? "unknown job";
        var zone = Data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(ClientState.TerritoryType)?.PlaceName.ValueNullable?.Name.ExtractText() ?? "unknown zone";
        var context = $"level {me.Level} {job}, in {zone}";
        unsafe
        {
            var game = FFXIVClientStructs.FFXIV.Client.Game.GameMain.Instance();
            if (game != null && game->CurrentContentFinderConditionId != 0 && Duties.ById.TryGetValue(game->CurrentContentFinderConditionId, out var duty))
                context += $", inside the duty {duty.Name}";
        }
        return context;
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
        PluginInterface.UiBuilder.Draw -= DrawUi;
        askEntry?.Remove();
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        CommandManager.RemoveHandler(Command);
        foreach (var c in askCommands)
            CommandManager.RemoveHandler(c);
        windows.RemoveAllWindows();
        Queue.Dispose();
        bossModGuard.Dispose();
        Follow.Dispose();
        Duty.Dispose();
        Icons.Dispose();
        messageRewriter.Dispose();
        tooltipRewriter.Dispose();
        addonRewriter.Dispose();
        textHook.Dispose();
    }
}
