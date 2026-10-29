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

    public Configuration Config { get; }
    public RenameEngine Engine { get; }

    private readonly WindowSystem windows = new("ActionRenamer");
    private readonly ConfigWindow configWindow;
    private readonly TextNodeHook textHook;
    private readonly AddonTextRewriter addonRewriter;
    private readonly TooltipRewriter tooltipRewriter;
    private readonly MessageRewriter messageRewriter;

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Engine = new RenameEngine(Config);

        textHook = new TextNodeHook(Interop, Log, Config, () => Engine);
        addonRewriter = new AddonTextRewriter(AddonLifecycle, Log, Config, () => Engine);
        tooltipRewriter = new TooltipRewriter(AddonLifecycle, GameGui, Log, Config);
        messageRewriter = new MessageRewriter(ChatGui, FlyTextGui, Config, () => Engine);

        configWindow = new ConfigWindow(this, Data);
        windows.AddWindow(configWindow);

        CommandManager.AddHandler(Command, new CommandInfo((_, _) => configWindow.Toggle())
        {
            HelpMessage = "Open Action Renamer settings.",
        });

        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    /// <summary>Saves settings and applies edits right away. Text already on screen updates when the game redraws it.</summary>
    public void SaveAndApply()
    {
        PluginInterface.SavePluginConfig(Config);
        Engine.Rebuild(Config);
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
        messageRewriter.Dispose();
        tooltipRewriter.Dispose();
        addonRewriter.Dispose();
        textHook.Dispose();
    }
}
