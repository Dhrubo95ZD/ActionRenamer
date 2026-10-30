using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace ActionRenamer;

/// <summary>Sci-fi names and descriptions for job stones ("Soul of the X" items, "Soul Crystal" category).</summary>
public static class JobCoreTheme
{
    private const uint StoneCategory = 62; // ItemUICategory "Soul Crystal"
    private const string Prefix = "Soul of the ";

    /// <summary>Game job name in "Soul of the X" to the name shown on the core.</summary>
    private static readonly Dictionary<string, string> Names = new()
    {
        ["Paladin"] = "Bulwark",
        ["Monk"] = "Striker",
        ["Warrior"] = "Vanguard",
        ["Dragoon"] = "Skylancer",
        ["Bard"] = "Signal Ranger",
        ["White Mage"] = "Field Medic",
        ["Black Mage"] = "Black Engineer",
        ["Summoner"] = "Drone Commander",
        ["Scholar"] = "Tactician",
        ["Ninja"] = "Infiltrator",
        ["Machinist"] = "Mech Engineer",
        ["Dark Knight"] = "Dark Trooper",
        ["Astrologian"] = "Astronomer",
        ["Carpenter"] = "Timber Fabricator",
        ["Blacksmith"] = "Weapon Forge",
        ["Armorer"] = "Armor Forge",
        ["Goldsmith"] = "Jewel Forge",
        ["Leatherworker"] = "Hide Fabricator",
        ["Weaver"] = "Textile Fabricator",
        ["Alchemist"] = "Chem Lab",
        ["Culinarian"] = "Galley",
        ["Red Mage"] = "Red Duelist",
        ["Samurai"] = "Ronin Blade",
        ["Blue Mage"] = "Blue Mimic",
        ["Gunbreaker"] = "Gunblade",
        ["Dancer"] = "Operative",
        ["Reaper"] = "Harvester",
        ["Sage"] = "Nano Medic",
        ["Pictomancer"] = "Holo-Artist",
        ["Viper"] = "Twinblade",
        ["Beastmaster"] = "Beast Handler",
    };

    /// <summary>Adds every job stone's name and description, read from the game's item sheet.</summary>
    public static void AddTo(Dictionary<string, string> map, IDataManager data)
    {
        map.TryAdd("Soul Crystal", "Job Core");
        map.TryAdd("Soul Crystals", "Job Cores");
        foreach (var item in data.GetExcelSheet<Item>())
        {
            if (item.ItemUICategory.RowId != StoneCategory)
                continue;
            var name = item.Name.ExtractText();
            if (!name.StartsWith(Prefix) || !Names.TryGetValue(name[Prefix.Length..], out var shown))
                continue;
            map.TryAdd(name, $"{shown} Core");
            var description = item.Description.ExtractText();
            if (description.Length > 0)
                map.TryAdd(description, $"This job core stores the operating records of past {shown.ToLowerInvariant()} units.");
        }
    }
}
