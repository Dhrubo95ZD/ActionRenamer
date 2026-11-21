using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace ActionRenamer;

/// <summary>One duty you can queue for through the Duty Finder.</summary>
public sealed record DutyEntry(
    uint CfcId, string Name, string Kind, string Expansion, int Level, int ItemLevel, uint InstanceContentId, uint SortKey);

/// <summary>
/// Every duty the Duty Finder can queue for, read from the game's own tables and grouped the way the
/// Duty Finder groups them: kind (dungeons, trials, raids...) first, then expansion.
/// </summary>
public sealed partial class DutyCatalog
{
    /// <summary>Order the kinds are shown in.</summary>
    public static readonly string[] KindOrder =
    {
        "Dungeons", "Guildhests", "Trials", "High-end Trials", "Normal Raids", "Alliance Raids", "Chaotic Alliance Raid",
        "Savage Raids", "Ultimate Raids",
    };

    public static readonly string[] ExpansionOrder =
    {
        "A Realm Reborn", "Heavensward", "Stormblood", "Shadowbringers", "Endwalker", "Dawntrail", "",
    };

    /// <summary>Duty Finder content without a Duty Finder category of its own, by content type name.</summary>
    private static readonly HashSet<string> UncategorisedKinds = new(StringComparer.Ordinal)
    {
        "Ultimate Raids", "Guildhests", "Chaotic Alliance Raid", "Trials",
    };

    public IReadOnlyList<DutyEntry> All { get; }
    public IReadOnlyDictionary<uint, DutyEntry> ById { get; }

    public DutyCatalog(IDataManager data)
    {
        var list = new List<DutyEntry>();
        foreach (var row in data.GetExcelSheet<ContentFinderCondition>())
        {
            var name = row.Name.ExtractText();
            if (name.Length == 0 || row.PvP)
                continue;

            string kind, expansion;
            var category = row.ContentUICategory.ValueNullable?.Name.ExtractText() ?? string.Empty;
            if (category.Length > 0)
            {
                var m = CategoryPattern().Match(category);
                if (!m.Success)
                    continue;
                kind = m.Groups[1].Value;
                expansion = m.Groups[2].Value;
                if (kind == "Raids")
                    kind = "Normal Raids";
            }
            else
            {
                kind = row.ContentType.ValueNullable?.Name.ExtractText() ?? string.Empty;
                if (!UncategorisedKinds.Contains(kind))
                    continue;
                if (kind == "Trials" && row.HighEndDuty)
                    kind = "High-end Trials";
                expansion = string.Empty;
            }
            if (!KindOrder.Contains(kind))
                continue;

            list.Add(new DutyEntry(
                row.RowId, Capitalise(name), kind, expansion, row.ClassJobLevelRequired, row.ItemLevelRequired,
                row.ContentLinkType == 1 ? row.Content.RowId : 0, row.SortKey));
        }

        All = list
            .OrderBy(d => Array.IndexOf(KindOrder, d.Kind))
            .ThenBy(d => ExpansionRank(d.Expansion))
            .ThenBy(d => d.Level)
            .ThenBy(d => d.SortKey)
            .ToList();
        ById = All.ToDictionary(d => d.CfcId);
    }

    public static int ExpansionRank(string expansion)
    {
        var i = Array.IndexOf(ExpansionOrder, expansion);
        return i < 0 ? ExpansionOrder.Length : i;
    }

    private static string Capitalise(string s) => s.Length > 0 && char.IsLower(s[0]) ? char.ToUpperInvariant(s[0]) + s[1..] : s;

    [GeneratedRegex(@"^(.*?) \((.*)\)$")]
    private static partial Regex CategoryPattern();
}
