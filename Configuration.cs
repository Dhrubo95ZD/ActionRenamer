using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace ActionRenamer;

/// <summary>How a rule matches text.</summary>
public enum MatchMode
{
    /// <summary>The whole text piece must equal Find (best for action names: leaves descriptions alone).</summary>
    Exact,
    /// <summary>Find must stand as a whole word or phrase.</summary>
    WholeWord,
    /// <summary>Find is replaced anywhere, even inside other words.</summary>
    Anywhere,
}

/// <summary>One find/replace rule.</summary>
[Serializable]
public class RenameRule
{
    public bool Enabled { get; set; } = true;
    public string Find { get; set; } = string.Empty;
    public string Replace { get; set; } = string.Empty;
    public MatchMode Mode { get; set; } = MatchMode.Exact;
    public bool CaseSensitive { get; set; } = true;
    /// <summary>Free label for grouping, e.g. a job name.</summary>
    public string Group { get; set; } = string.Empty;
}

/// <summary>Custom name and description for one action, keyed by action id.</summary>
[Serializable]
public class ActionOverride
{
    /// <summary>Game name at the time the override was made; used to find the name on screen.</summary>
    public string OriginalName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool HideDescription { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Description) && !HideDescription;
}

/// <summary>Saved plugin settings.</summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public bool Enabled { get; set; } = true;
    /// <summary>All game UI text: windows, tooltips, cast bars, party list.</summary>
    public bool ApplyToUiText { get; set; } = true;
    public bool ApplyToChat { get; set; } = true;
    public bool ApplyToFlyText { get; set; } = true;
    /// <summary>Blank the description of every action tooltip that has no custom description.</summary>
    public bool HideAllDescriptions { get; set; }
    /// <summary>Write each replacement to dalamud.log (for troubleshooting).</summary>
    public bool LogChanges { get; set; }

    /// <summary>Install the bundled sci-fi icon mod into Penumbra automatically.</summary>
    public bool AutoInstallIcons { get; set; } = true;

    /// <summary>Icon pack version last installed into Penumbra.</summary>
    public string InstalledIconPackVersion { get; set; } = string.Empty;

    /// <summary>Rename job names and every skill and trait with the built-in sci-fi theme.</summary>
    public bool SkillThemeEnabled { get; set; } = true;

    /// <summary>Rename all status effects (buffs and debuffs) with the built-in sci-fi theme.</summary>
    public bool StatusThemeEnabled { get; set; } = true;

    /// <summary>Your own status names: game name -> new name. Wins over the theme; empty value keeps the game name.</summary>
    public Dictionary<string, string> StatusOverrides { get; set; } = new();

    /// <summary>Keyed by action id; traits use <see cref="TraitKey"/>.</summary>
    public Dictionary<uint, ActionOverride> Overrides { get; set; } = new();

    /// <summary>Overrides key for a trait, kept apart from action ids.</summary>
    public static uint TraitKey(uint traitId) => 0x4000_0000u | traitId;
    public List<RenameRule> Rules { get; set; } = Presets.Default();
}
