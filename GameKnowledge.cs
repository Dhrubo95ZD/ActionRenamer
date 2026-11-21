using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace ActionRenamer;

/// <summary>
/// Searches the game's own data for the words in a question: duties, quests (with where they start) and items.
/// The matches go to Claude with the question, so answers come from this game version rather than memory.
/// </summary>
public sealed class GameKnowledge
{
    private sealed record Entry(string Name, string NameLower, string Text);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "how", "what", "where", "when", "which", "who", "why", "can", "does", "did", "get", "got",
        "find", "unlock", "unlocks", "unlocked", "level", "need", "want", "with", "from", "this", "that", "there", "you",
        "your", "are", "was", "have", "has", "into", "about", "best", "good", "should", "would", "could", "give", "make",
        "use", "using", "any", "all", "some", "out", "start", "starts", "quest", "item", "items", "duty", "first", "last", "next",
    };

    private readonly IDataManager data;
    private readonly DutyCatalog duties;
    private List<Entry>? quests;
    private List<Entry>? items;
    private readonly object gate = new();

    public GameKnowledge(IDataManager data, DutyCatalog duties)
    {
        this.data = data;
        this.duties = duties;
    }

    /// <summary>Game data lines that match the question, at most <paramref name="max"/> per kind. Safe to call off the game thread.</summary>
    public string Lookup(string question, int max = 8)
    {
        var words = question
            .Split(new[] { ' ', ',', '.', '?', '!', ':', ';', '"', '(', ')', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('\'').ToLowerInvariant())
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .Distinct()
            .ToList();
        if (words.Count == 0)
            return string.Empty;

        EnsureLoaded();
        var sb = new StringBuilder();

        // Duties match on name, type and expansion ("heavensward dungeon"), then list lowest level first.
        var dutyHits = duties.All
            .Select(d => (d, score: Score(d.Name.ToLowerInvariant(), words) * 3 + Score($"{d.Kind} {d.Expansion}".ToLowerInvariant(), words)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score).ThenBy(x => x.d.Level).ThenBy(x => x.d.SortKey)
            .Take(max)
            .OrderBy(x => x.d.Level).ThenBy(x => x.d.SortKey);
        foreach (var (d, _) in dutyHits)
            sb.AppendLine($"Duty: {d.Name} - {d.Kind}{(d.Expansion.Length > 0 ? $" ({d.Expansion})" : "")}, level {d.Level}{(d.ItemLevel > 0 ? $", item level {d.ItemLevel}" : "")}");

        foreach (var e in Best(quests!, words, max))
            sb.AppendLine(e.Text);
        foreach (var e in Best(items!, words, max))
            sb.AppendLine(e.Text);

        return sb.ToString();
    }

    private static IEnumerable<Entry> Best(List<Entry> list, List<string> words, int max) => list
        .Select(e => (e, score: Score(e.NameLower, words)))
        .Where(x => x.score > 0)
        .OrderByDescending(x => x.score).ThenBy(x => x.e.Name.Length)
        .Take(max)
        .Select(x => x.e);

    /// <summary>How many of the question's words the name contains; whole-name matches count extra.</summary>
    private static int Score(string nameLower, List<string> words)
    {
        var score = words.Count(w => nameLower.Contains(w, StringComparison.Ordinal));
        if (score > 0 && words.Count >= 2 && string.Join(' ', words).Contains(nameLower, StringComparison.Ordinal))
            score += 3;
        return score;
    }

    private void EnsureLoaded()
    {
        lock (gate)
        {
            if (quests != null)
                return;
            quests = LoadQuests();
            items = LoadItems();
        }
    }

    private List<Entry> LoadQuests()
    {
        var npcs = data.GetExcelSheet<ENpcResident>();
        var list = new List<Entry>();
        foreach (var q in data.GetExcelSheet<Quest>())
        {
            var name = q.Name.ExtractText();
            if (name.Length == 0)
                continue;
            var level = q.ClassJobLevel.Count > 0 ? q.ClassJobLevel[0] : 0;
            var giver = npcs.GetRowOrDefault(q.IssuerStart.RowId)?.Singular.ExtractText() ?? string.Empty;
            var where = Where(q.IssuerLocation.ValueNullable);
            var genre = q.JournalGenre.ValueNullable?.Name.ExtractText() ?? string.Empty;
            var text = $"Quest: {name} - level {level}{(genre.Length > 0 ? $", {genre}" : "")}"
                + (giver.Length > 0 ? $", from {Capitalise(giver)}" : "")
                + (where.Length > 0 ? $" in {where}" : "");
            list.Add(new Entry(name, name.ToLowerInvariant(), text));
        }
        return list;
    }

    private List<Entry> LoadItems()
    {
        var list = new List<Entry>();
        foreach (var i in data.GetExcelSheet<Item>())
        {
            var name = i.Name.ExtractText();
            if (name.Length == 0)
                continue;
            var category = i.ItemUICategory.ValueNullable?.Name.ExtractText() ?? string.Empty;
            var text = $"Item: {name}{(category.Length > 0 ? $" - {category}" : "")}"
                + (i.LevelItem.RowId > 1 ? $", item level {i.LevelItem.RowId}" : "")
                + (i.LevelEquip > 1 ? $", equip level {i.LevelEquip}" : "");
            list.Add(new Entry(name, name.ToLowerInvariant(), text));
        }
        return list;
    }

    /// <summary>"Zone (x, y)" for a Level row, using the game's map coordinate formula.</summary>
    private static string Where(Level? level)
    {
        if (level is not { } l)
            return string.Empty;
        var map = l.Map.ValueNullable;
        var zone = l.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText()
            ?? map?.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty;
        if (map is not { } m || m.SizeFactor == 0)
            return zone;
        var x = MapCoordinate(l.X, m.OffsetX, m.SizeFactor);
        var y = MapCoordinate(l.Z, m.OffsetY, m.SizeFactor);
        return $"{zone} ({x:0.0}, {y:0.0})";
    }

    private static double MapCoordinate(float position, short offset, ushort sizeFactor)
    {
        var scale = sizeFactor / 100.0;
        var raw = (position + offset) * scale;
        return 41.0 / scale * ((raw + 1024.0) / 2048.0) + 1.0;
    }

    private static string Capitalise(string s) => s.Length > 0 && char.IsLower(s[0]) ? char.ToUpperInvariant(s[0]) + s[1..] : s;
}
