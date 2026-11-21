using System.Collections.Generic;

namespace ActionRenamer;

/// <summary>One plugin in the saved setup.</summary>
public sealed record SetupPlugin(string InternalName, string Name, string About, string Repo);

/// <summary>
/// The plugins and custom repositories this player had installed on 21 Nov 2026, shipped with the plugin
/// so they can be found again after a fresh install. Generated from the installed plugins and dalamudConfig.json.
/// </summary>
public static class MySetup
{
    public static readonly IReadOnlyList<string> Repositories = new[]
    {
        "https://raw.githubusercontent.com/Dhrubo95ZD/ActionRenamer/main/repo.json",
        "https://raw.githubusercontent.com/xivdev/Penumbra/master/repo.json",
        "https://raw.githubusercontent.com/1Stalk/DalamudPlugins/main/pluginmaster.json",
        "https://love.puni.sh/ment.json",
        "https://puni.sh/api/repository/veyn",
        "https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json",
        "https://puni.sh/api/repository/ice",
        "https://puni.sh/api/repository/erdelf",
        "https://puni.sh/api/repository/croizat",
        "https://puni.sh/api/repository/aetherlove",
        "https://puni.sh/api/repository/vera",
    };

    public static readonly IReadOnlyList<SetupPlugin> Plugins = new[]
    {
        new SetupPlugin("ActionRenamer", "Action Renamer", "Sci-fi names and icons for skills, jobs and status effects.", "https://raw.githubusercontent.com/Dhrubo95ZD/ActionRenamer/main/repo.json"),
        new SetupPlugin("Artisan", "Artisan", "A 'simple' crafting plugin.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("AutoDuty", "AutoDuty", "Dungeons while you sleep, eat, or work. (Swimsuit Shtola made easy.)", "https://puni.sh/api/repository/erdelf"),
        new SetupPlugin("AutoHook", "AutoHook", "Makes fishing less (or more) boring", "https://love.puni.sh/ment.json"),
        new SetupPlugin("AutoKeyboundBrawler", "AutoKeyboundBrawler", "Automated assistant and typing solver for Keybound Brawler.", "https://raw.githubusercontent.com/1Stalk/DalamudPlugins/main/pluginmaster.json"),
        new SetupPlugin("AutoRetainer", "AutoRetainer", "Collect and assign ventures to your retainers from the comfort of your bed.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("Avarice", "Avarice", "Positional feedback with pixel perfect rear/flank tracking, range indicators, and more.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("BossMod", "Boss Mod", "Don't fail at raid mechanics anymore", "https://puni.sh/api/repository/veyn"),
        new SetupPlugin("ChilledLeves", "Chilled Leves", "I heard you like your leves on the rocks.", "https://puni.sh/api/repository/ice"),
        new SetupPlugin("Deliveroo", "Deliveroo", "Better Grand Company Deliveries", "https://puni.sh/api/repository/vera"),
        new SetupPlugin("ICE", "Ice's Cosmic Exploration", "Welcome to, the dark side of, the MOOOON", "https://puni.sh/api/repository/ice"),
        new SetupPlugin("LazyLoot", "LazyLoot", "Dungeon Loot but Lazy.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("Lifestream", "Lifestream", "A plugin to speed up Aethernet travel and World changing.", "https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json"),
        new SetupPlugin("Marketbuddy", "Marketbuddy", "Less clicks, more gil!", "https://love.puni.sh/ment.json"),
        new SetupPlugin("Orbwalker", "Orbwalker", "S P A C E G L I D I N G", "https://love.puni.sh/ment.json"),
        new SetupPlugin("Palantir", "Palantir", "Lost in a deep dungeon with nothing but dry bread and water?", "https://love.puni.sh/ment.json"),
        new SetupPlugin("PandorasBox", "Pandora's Box", "Do you dare to open Pandora's Box?", "https://love.puni.sh/ment.json"),
        new SetupPlugin("Penumbra", "Penumbra", "Runtime mod loader and manager.", "https://raw.githubusercontent.com/xivdev/Penumbra/master/repo.json"),
        new SetupPlugin("Questionable", "Questionable", "A tiny quest helper plugin.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("Saucy", "Saucy", "When working for MGP is too much effort.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("SomethingNeedDoing", "SomethingNeedDoing", "Work work.", "https://puni.sh/api/repository/croizat"),
        new SetupPlugin("Splatoon", "Splatoon", "Scriptable overlay engine for drawing mechanics, markers, and indicators in real-time", "https://love.puni.sh/ment.json"),
        new SetupPlugin("TextAdvance", "TextAdvance", "Quest speedrunning assistance tool. Automatically skips subtitle boxes and confirms cutscene skipping", "https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json"),
        new SetupPlugin("visland", "Visland", "Automate island sanctuary activities", "https://puni.sh/api/repository/veyn"),
        new SetupPlugin("vnavmesh", "vnavmesh", "Move around!", "https://puni.sh/api/repository/veyn"),
        new SetupPlugin("vsatisfy", "vsatisfy", "Simplify custom deliveries.", "https://puni.sh/api/repository/veyn"),
        new SetupPlugin("WrathCombo", "Wrath Combo", "Condenses combos and mutually exclusive abilities onto a single button - and then some.", "https://love.puni.sh/ment.json"),
        new SetupPlugin("YesAlready", "YesAlready", "Clicks Yes in various dialogs you've specified.", "https://love.puni.sh/ment.json"),
    };
}
