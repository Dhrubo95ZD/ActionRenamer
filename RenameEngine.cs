using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ActionRenamer;

/// <summary>Applies the rule list to plain strings and SeStrings. Rebuild after rules change.</summary>
public sealed class RenameEngine
{
    private readonly List<(RenameRule Rule, Regex Regex)> wordRules = new();
    private readonly Dictionary<string, string> exact = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> exactIgnoreCase = new(StringComparer.OrdinalIgnoreCase);
    // Theme names (skills, traits, jobs, statuses) that may sit inside sentences.
    private readonly Dictionary<string, string> statusNames = new(StringComparer.Ordinal);

    public RenameEngine(Configuration config, IReadOnlyDictionary<string, string> statusMap) => Rebuild(config, statusMap);

    /// <param name="statusMap">Status name -> new name; only entries that differ.</param>
    public void Rebuild(Configuration config, IReadOnlyDictionary<string, string> statusMap)
    {
        wordRules.Clear();
        exact.Clear();
        exactIgnoreCase.Clear();
        statusNames.Clear();

        // Exact names: first one wins, so skill edits beat status names, which beat exact word rules.
        foreach (var ov in config.Overrides.Values)
        {
            if (!string.IsNullOrWhiteSpace(ov.Name) && !string.IsNullOrEmpty(ov.OriginalName))
                exact.TryAdd(ov.OriginalName, ov.Name);
        }
        foreach (var (from, to) in statusMap)
        {
            exact.TryAdd(from, to);
            statusNames[from] = to;
        }

        foreach (var rule in config.Rules)
        {
            if (!rule.Enabled || string.IsNullOrEmpty(rule.Find))
                continue;

            if (rule.Mode == MatchMode.Exact)
            {
                if (rule.CaseSensitive) exact.TryAdd(rule.Find, rule.Replace);
                else exactIgnoreCase.TryAdd(rule.Find, rule.Replace);
                continue;
            }

            var pattern = Regex.Escape(rule.Find);
            if (rule.Mode == MatchMode.WholeWord)
                pattern = $@"(?<![\w']){pattern}(?![\w'])";
            var options = RegexOptions.CultureInvariant | (rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            wordRules.Add((rule, new Regex(pattern, options)));
        }

        // Very short names ("Ten", "Chi") are left out of sentence matching so ordinary words stay intact.
        maxPhraseWords = 1;
        foreach (var key in statusNames.Keys.Where(k => k.Length < 4).ToList())
            statusNames.Remove(key);
        foreach (var key in statusNames.Keys)
            maxPhraseWords = Math.Max(maxPhraseWords, key.Split(' ').Length);
    }

    private int maxPhraseWords = 1;

    /// <summary>
    /// Replaces names inside sentences ("You use Ouroboros."). At each word start, tries the longest run of
    /// words first and looks it up in the name table: one dictionary lookup per candidate, no giant regex.
    /// </summary>
    private string ReplacePhrases(string text)
    {
        if (statusNames.Count == 0)
            return text;

        // Word spans: letters, digits, apostrophes and inner hyphens/colons belong to a word.
        var starts = new List<int>();
        var ends = new List<int>();
        for (var i = 0; i < text.Length;)
        {
            if (!IsWordChar(text[i])) { i++; continue; }
            var s = i;
            while (i < text.Length && (IsWordChar(text[i]) || ((text[i] == '-' || text[i] == ':') && i + 1 < text.Length && IsWordChar(text[i + 1]))))
                i++;
            starts.Add(s);
            ends.Add(i);
        }

        System.Text.StringBuilder? sb = null;
        var copied = 0;
        for (var w = 0; w < starts.Count; w++)
        {
            for (var n = Math.Min(maxPhraseWords, starts.Count - w); n >= 1; n--)
            {
                var start = starts[w];
                var end = ends[w + n - 1];
                var candidate = text.Substring(start, end - start);
                if (!statusNames.TryGetValue(candidate, out var replacement))
                    continue;
                sb ??= new System.Text.StringBuilder(text.Length + 16);
                sb.Append(text, copied, start - copied).Append(replacement);
                copied = end;
                w += n - 1;
                break;
            }
        }
        if (sb == null)
            return text;
        sb.Append(text, copied, text.Length - copied);
        return sb.ToString();
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '\'';

    public bool IsEmpty => wordRules.Count == 0 && exact.Count == 0 && exactIgnoreCase.Count == 0;

    /// <summary>Returns the rewritten text, or the same instance when nothing matched.</summary>
    /// <param name="sentences">True for chat and fly text, where status names sit inside sentences.</param>
    public string Apply(string text, bool sentences = false)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Exact names match a whole line ("Ouroboros" in "Ouroboros\rLv. 96"), so list entries work too.
        var result = ApplyExactPerLine(text);
        if (sentences)
            result = ReplacePhrases(result);
        foreach (var (rule, regex) in wordRules)
            result = regex.Replace(result, rule.Replace.Replace("$", "$$"));
        return result;
    }

    private bool TryExact(string text, out string replacement) =>
        exact.TryGetValue(text, out replacement!) || exactIgnoreCase.TryGetValue(text, out replacement!);

    private string ApplyExactPerLine(string text)
    {
        if (text.IndexOfAny(new[] { '\r', '\n' }) < 0)
        {
            var t = text.Trim();
            return t.Length > 0 && TryExact(t, out var r) ? text.Replace(t, r) : text;
        }

        var lines = text.Split('\r', '\n');
        var changed = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || !TryExact(trimmed, out var replacement))
                continue;
            lines[i] = lines[i].Replace(trimmed, replacement);
            changed = true;
        }
        if (!changed)
            return text;

        // Put the original separators back.
        var sb = new System.Text.StringBuilder(text.Length + 16);
        var line = 0;
        sb.Append(lines[0]);
        foreach (var ch in text)
        {
            if (ch != '\r' && ch != '\n')
                continue;
            sb.Append(ch);
            sb.Append(lines[++line]);
        }
        return sb.ToString();
    }

    /// <summary>True when the text holds a renamed name or a rule's Find text; used for debug logging.</summary>
    public bool Mentions(string text)
    {
        foreach (var line in text.Split('\r', '\n'))
            if (TryExact(line.Trim(), out _))
                return true;
        foreach (var (rule, _) in wordRules)
            if (text.Contains(rule.Find, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>Rewrites the text payloads of an SeString in place. Returns true when anything changed.</summary>
    public bool Apply(SeString seString, bool sentences = false)
    {
        var changed = false;
        foreach (var payload in seString.Payloads)
        {
            if (payload is not TextPayload text || text.Text == null)
                continue;
            var replaced = Apply(text.Text, sentences);
            if (!ReferenceEquals(replaced, text.Text) && replaced != text.Text)
            {
                text.Text = replaced;
                changed = true;
            }
        }
        return changed;
    }
}
