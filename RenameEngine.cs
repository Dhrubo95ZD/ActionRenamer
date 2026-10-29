using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ActionRenamer;

/// <summary>Applies the rule list to plain strings and SeStrings. Rebuild after rules change.</summary>
public sealed class RenameEngine
{
    private readonly List<(RenameRule Rule, Regex? Regex)> compiled = new();

    public RenameEngine(Configuration config) => Rebuild(config);

    public void Rebuild(Configuration config)
    {
        compiled.Clear();

        // Per-skill names come first as exact rules, so they beat the word rules.
        var rules = new List<RenameRule>();
        foreach (var ov in config.Overrides.Values)
        {
            if (!string.IsNullOrWhiteSpace(ov.Name) && !string.IsNullOrEmpty(ov.OriginalName))
                rules.Add(new RenameRule { Find = ov.OriginalName, Replace = ov.Name, Mode = MatchMode.Exact });
        }
        rules.AddRange(config.Rules);

        foreach (var rule in rules)
        {
            if (!rule.Enabled || string.IsNullOrEmpty(rule.Find))
                continue;

            Regex? regex = null;
            if (rule.Mode != MatchMode.Exact)
            {
                var pattern = Regex.Escape(rule.Find);
                if (rule.Mode == MatchMode.WholeWord)
                    pattern = $@"(?<![\w']){pattern}(?![\w'])";
                var options = RegexOptions.CultureInvariant | (rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                regex = new Regex(pattern, options);
            }
            compiled.Add((rule, regex));
        }
    }

    public bool IsEmpty => compiled.Count == 0;

    /// <summary>Returns the rewritten text, or the same instance when nothing matched.</summary>
    public string Apply(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Exact rules match a whole line ("Ouroboros" in "Ouroboros\rLv. 96"), so list entries work too.
        var result = ApplyExactPerLine(text);
        foreach (var (rule, regex) in compiled)
        {
            if (regex != null)
                result = regex.Replace(result, rule.Replace.Replace("$", "$$"));
        }
        return result;
    }

    private string ApplyExactPerLine(string text)
    {
        var lines = text.Split('\r', '\n');
        var changed = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0)
                continue;
            foreach (var (rule, regex) in compiled)
            {
                if (regex != null)
                    continue;
                var cmp = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (string.Equals(trimmed, rule.Find, cmp))
                {
                    lines[i] = lines[i].Replace(trimmed, rule.Replace);
                    changed = true;
                    break;
                }
            }
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

    /// <summary>True when the text holds the Find text of any rule; used for debug logging.</summary>
    public bool Mentions(string text)
    {
        foreach (var (rule, _) in compiled)
            if (text.Contains(rule.Find, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>Rewrites the text payloads of an SeString in place. Returns true when anything changed.</summary>
    public bool Apply(SeString seString)
    {
        var changed = false;
        foreach (var payload in seString.Payloads)
        {
            if (payload is not TextPayload text || text.Text == null)
                continue;
            var replaced = Apply(text.Text);
            if (!ReferenceEquals(replaced, text.Text) && replaced != text.Text)
            {
                text.Text = replaced;
                changed = true;
            }
        }
        return changed;
    }
}
