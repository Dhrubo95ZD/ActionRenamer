using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace ActionRenamer;

/// <summary>Settings: per-skill editor, word rules, and switches.</summary>
public sealed class ConfigWindow : Window
{
    private record ActionEntry(uint Id, string Name, HashSet<uint> Jobs, bool IsTrait)
    {
        /// <summary>Key in Configuration.Overrides (traits live in their own id range).</summary>
        public uint Key => IsTrait ? Configuration.TraitKey(Id) : Id;
        /// <summary>Name as the game shows it, without our (PvP)/(trait) tags.</summary>
        public string GameName => Name.EndsWith(" (PvP)") ? Name[..^6] : Name.EndsWith(" (trait)") ? Name[..^8] : Name;
    }

    private readonly Plugin plugin;
    private readonly List<ActionEntry> actions;
    private readonly List<(uint Id, string Name)> jobs;

    private string ruleFilter = string.Empty;
    private string skillSearch = string.Empty;
    private int jobIndex; // 0 = all jobs
    private bool onlyEdited;
    private string statusSearch = string.Empty;
    private bool onlyRenamedStatuses;

    public ConfigWindow(Plugin plugin, IDataManager data) : base("Action Renamer###ActionRenamerConfig")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(700, 420), MaximumSize = new Vector2(4000, 4000) };

        var classJobs = data.GetExcelSheet<ClassJob>().Where(j => j.RowId > 0 && !j.Name.IsEmpty).ToList();

        // A job also uses its base class's skills (Black Mage gets Thaumaturge skills).
        var jobsForClass = new Dictionary<uint, HashSet<uint>>();
        foreach (var j in classJobs)
        {
            Add(j.RowId, j.RowId);
            if (j.ClassJobParent.RowId != 0) Add(j.ClassJobParent.RowId, j.RowId);
        }
        void Add(uint cls, uint job)
        {
            if (!jobsForClass.TryGetValue(cls, out var set)) jobsForClass[cls] = set = new HashSet<uint>();
            set.Add(job);
        }

        // ClassJobCategory has one bool column per job abbreviation (VPR, BLM, ...).
        var categoryColumns = classJobs
            .Select(j => (j.RowId, Prop: typeof(ClassJobCategory).GetProperty(j.Abbreviation.ExtractText())))
            .Where(c => c.Prop != null && c.Prop.PropertyType == typeof(bool))
            .ToList();

        // Every action a job can see: hotbar skills, combo follow-ups, replaced and PvP actions.
        actions = new List<ActionEntry>();
        foreach (var a in data.GetExcelSheet<Lumina.Excel.Sheets.Action>())
        {
            var name = a.Name.ExtractText();
            if (name.Length == 0)
                continue;

            var owners = new HashSet<uint>();
            if (a.ClassJob.RowId != 0 && jobsForClass.TryGetValue(a.ClassJob.RowId, out var fromClass))
                owners.UnionWith(fromClass);

            // Category 1 is "All Classes"; NPC actions use it too, so trust it only on player actions.
            var category = a.ClassJobCategory.ValueNullable;
            if (category != null && (a.ClassJobCategory.RowId > 1 || a.IsPlayerAction))
            {
                foreach (var (jobId, prop) in categoryColumns)
                    if ((bool)prop!.GetValue(category!.Value)!)
                        owners.Add(jobId);
            }

            if (owners.Count == 0 && !a.IsPlayerAction)
                continue;
            actions.Add(new ActionEntry(a.RowId, a.IsPvP ? $"{name} (PvP)" : name, owners, false));
        }

        // Traits (passive skills shown in Actions & Traits).
        foreach (var t in data.GetExcelSheet<Trait>())
        {
            var name = t.Name.ExtractText();
            if (name.Length == 0 || t.ClassJob.RowId == 0)
                continue;
            var owners = jobsForClass.TryGetValue(t.ClassJob.RowId, out var set) ? set : new HashSet<uint>();
            actions.Add(new ActionEntry(t.RowId, $"{name} (trait)", owners, true));
        }
        actions.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

        jobs = new List<(uint, string)> { (uint.MaxValue, "All jobs") };
        jobs.AddRange(classJobs
            .Select(j => (j.RowId, Capitalise(j.Name.ExtractText())))
            .OrderBy(j => j.Item2));
    }

    public override void Draw()
    {
        var cfg = plugin.Config;
        var dirty = false;

        dirty |= Check("Enabled", () => cfg.Enabled, v => cfg.Enabled = v);
        ImGui.SameLine(); dirty |= Check("Game UI text", () => cfg.ApplyToUiText, v => cfg.ApplyToUiText = v);
        ImGui.SameLine(); dirty |= Check("Chat", () => cfg.ApplyToChat, v => cfg.ApplyToChat = v);
        ImGui.SameLine(); dirty |= Check("Fly text", () => cfg.ApplyToFlyText, v => cfg.ApplyToFlyText = v);
        ImGui.SameLine(); dirty |= Check("Hide ALL skill descriptions", () => cfg.HideAllDescriptions, v => cfg.HideAllDescriptions = v);
        ImGui.SameLine(); dirty |= Check("Log changes", () => cfg.LogChanges, v => cfg.LogChanges = v);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Writes each replacement to dalamud.log, for troubleshooting.");

        if (ImGui.BeginTabBar("tabs"))
        {
            if (ImGui.BeginTabItem("Skills"))
            {
                dirty |= DrawSkills(cfg);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Status effects"))
            {
                dirty |= DrawStatuses(cfg);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Word rules"))
            {
                dirty |= DrawRules(cfg);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        if (dirty)
            plugin.SaveAndApply();
    }

    private bool DrawSkills(Configuration cfg)
    {
        var dirty = false;
        dirty |= Check("Sci-fi theme for job names, skills and traits", () => cfg.SkillThemeEnabled, v => cfg.SkillThemeEnabled = v);
        ImGui.TextWrapped("Grey text in \"New name\" is the theme name. Type to use your own instead. Empty description keeps the game text. Changes show the next time the game redraws that text (re-hover the skill or reopen the window).");

        ImGui.SetNextItemWidth(180);
        if (ImGui.BeginCombo("##job", jobs[jobIndex].Name))
        {
            for (var i = 0; i < jobs.Count; i++)
                if (ImGui.Selectable(jobs[i].Name, i == jobIndex))
                    jobIndex = i;
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##search", "Search skill name...", ref skillSearch, 64);
        ImGui.SameLine();
        ImGui.Checkbox("Only edited", ref onlyEdited);

        var jobId = jobs[jobIndex].Id;
        var list = actions.Where(a =>
            (jobId == uint.MaxValue || a.Jobs.Contains(jobId))
            && (skillSearch.Length == 0 || a.Name.Contains(skillSearch, StringComparison.OrdinalIgnoreCase))
            && (!onlyEdited || cfg.Overrides.ContainsKey(a.Key)));

        if (jobId == uint.MaxValue && skillSearch.Length == 0 && !onlyEdited)
        {
            ImGui.TextDisabled("Choose a job or search to list skills.");
            return false;
        }

        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable;
        if (!ImGui.BeginTable("skills", 4, flags, new Vector2(0, ImGui.GetContentRegionAvail().Y)))
            return false;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Game name", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("New name", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("New description", ImGuiTableColumnFlags.WidthStretch, 2f);
        ImGui.TableSetupColumn("Hide desc.", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableHeadersRow();

        foreach (var a in list.Take(400))
        {
            ImGui.PushID((int)a.Key);
            ImGui.TableNextRow();
            cfg.Overrides.TryGetValue(a.Key, out var ov);
            var name = ov?.Name ?? string.Empty;
            var description = ov?.Description ?? string.Empty;
            var hide = ov?.HideDescription ?? false;
            var changed = false;

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(a.Name);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(a.IsTrait ? $"Trait id {a.Id}" : $"Action id {a.Id}");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            changed |= ImGui.InputTextWithHint("##name", plugin.SkillThemeName(a.GameName), ref name, 64);

            ImGui.TableNextColumn();
            var hasGameText = TooltipRewriter.GameDescriptions.TryGetValue(a.Key, out var gameText);
            if (!hasGameText) ImGui.BeginDisabled();
            if (ImGui.SmallButton("Game text") && hasGameText)
            {
                description = gameText!;
                changed = true;
            }
            if (!hasGameText) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(hasGameText ? "Copy the game's description here so you can edit it." : "Hover this skill in game once, then this button copies its description.");
            ImGui.SetNextItemWidth(-1);
            changed |= ImGui.InputTextMultiline("##desc", ref description, 2000, new Vector2(-1, ImGui.GetTextLineHeight() * 4));

            ImGui.TableNextColumn();
            changed |= ImGui.Checkbox("##hide", ref hide);

            if (changed)
            {
                ov ??= new ActionOverride();
                ov.OriginalName = a.GameName;
                ov.Name = name;
                ov.Description = description;
                ov.HideDescription = hide;
                if (ov.IsEmpty) cfg.Overrides.Remove(a.Key);
                else cfg.Overrides[a.Key] = ov;
                dirty = true;
            }
            ImGui.PopID();
        }
        ImGui.EndTable();
        return dirty;
    }

    private bool DrawStatuses(Configuration cfg)
    {
        var dirty = Check("Sci-fi theme for all buffs and debuffs", () => cfg.StatusThemeEnabled, v => cfg.StatusThemeEnabled = v);
        ImGui.TextWrapped("Type a name in \"Your name\" to override any status. Leave it empty to use the theme name. \"Game name\" keeps the original.");

        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##statussearch", "Search game or new name...", ref statusSearch, 64);
        ImGui.SameLine();
        ImGui.Checkbox("Only renamed", ref onlyRenamedStatuses);

        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable;
        if (!ImGui.BeginTable("statuses", 4, flags, new Vector2(0, ImGui.GetContentRegionAvail().Y)))
            return dirty;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Game name", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("Shown as", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("Your name", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 90);
        ImGui.TableHeadersRow();

        var shown = 0;
        for (var i = 0; i < plugin.StatusNames.Count && shown < 400; i++)
        {
            var name = plugin.StatusNames[i];
            var display = plugin.StatusDisplayName(name);
            if (onlyRenamedStatuses && display == name)
                continue;
            if (statusSearch.Length > 0
                && !name.Contains(statusSearch, StringComparison.OrdinalIgnoreCase)
                && !display.Contains(statusSearch, StringComparison.OrdinalIgnoreCase))
                continue;
            shown++;

            ImGui.PushID(i);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(name);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(display);

            ImGui.TableNextColumn();
            cfg.StatusOverrides.TryGetValue(name, out var custom);
            var hasOverride = custom != null;
            custom ??= string.Empty;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##custom", ref custom, 64))
            {
                if (custom.Length == 0) cfg.StatusOverrides.Remove(name);
                else cfg.StatusOverrides[name] = custom;
                dirty = true;
            }

            ImGui.TableNextColumn();
            if (ImGui.SmallButton("Game name"))
            {
                cfg.StatusOverrides[name] = name;
                dirty = true;
            }
            if (hasOverride)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("X"))
                {
                    cfg.StatusOverrides.Remove(name);
                    dirty = true;
                }
            }
            ImGui.PopID();
        }
        ImGui.EndTable();
        if (shown == 400)
            ImGui.TextDisabled("Showing the first 400. Search to narrow it down.");
        return dirty;
    }

    private bool DrawRules(Configuration cfg)
    {
        var dirty = false;
        ImGui.TextWrapped("Rules change words everywhere (class names, descriptions, chat). Exact = whole text must match. WholeWord = word or phrase inside text. Rules run top to bottom.");

        if (ImGui.Button("Add rule"))
        {
            cfg.Rules.Add(new RenameRule());
            dirty = true;
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset to preset") && ImGui.GetIO().KeyCtrl)
        {
            cfg.Rules = Presets.Default();
            dirty = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold Ctrl and click. Replaces all word rules (skill edits are kept).");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##filter", "Filter...", ref ruleFilter, 64);

        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable;
        if (!ImGui.BeginTable("rules", 7, flags, new Vector2(0, ImGui.GetContentRegionAvail().Y)))
            return dirty;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed, 28);
        ImGui.TableSetupColumn("Group", ImGuiTableColumnFlags.WidthStretch, 0.7f);
        ImGui.TableSetupColumn("Find", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("Replace with", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Aa", ImGuiTableColumnFlags.WidthFixed, 28);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableHeadersRow();

        var modes = Enum.GetNames<MatchMode>();
        for (var i = 0; i < cfg.Rules.Count; i++)
        {
            var rule = cfg.Rules[i];
            if (ruleFilter.Length > 0
                && !rule.Find.Contains(ruleFilter, StringComparison.OrdinalIgnoreCase)
                && !rule.Replace.Contains(ruleFilter, StringComparison.OrdinalIgnoreCase)
                && !rule.Group.Contains(ruleFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var enabled = rule.Enabled;
            if (ImGui.Checkbox("##on", ref enabled)) { rule.Enabled = enabled; dirty = true; }

            dirty |= TextCell("##group", rule.Group, v => rule.Group = v);
            dirty |= TextCell("##find", rule.Find, v => rule.Find = v);
            dirty |= TextCell("##replace", rule.Replace, v => rule.Replace = v);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var mode = (int)rule.Mode;
            if (ImGui.Combo("##mode", ref mode, modes, modes.Length)) { rule.Mode = (MatchMode)mode; dirty = true; }

            ImGui.TableNextColumn();
            var cs = rule.CaseSensitive;
            if (ImGui.Checkbox("##case", ref cs)) { rule.CaseSensitive = cs; dirty = true; }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Case sensitive");

            ImGui.TableNextColumn();
            if (ImGui.ArrowButton("##up", ImGuiDir.Up) && i > 0)
            {
                (cfg.Rules[i - 1], cfg.Rules[i]) = (cfg.Rules[i], cfg.Rules[i - 1]);
                dirty = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("X"))
            {
                cfg.Rules.RemoveAt(i);
                dirty = true;
                ImGui.PopID();
                break;
            }

            ImGui.PopID();
        }
        ImGui.EndTable();
        return dirty;
    }

    private static string Capitalise(string s) =>
        string.Join(' ', s.Split(' ').Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));

    private static bool Check(string label, Func<bool> get, Action<bool> set)
    {
        var v = get();
        if (!ImGui.Checkbox(label, ref v)) return false;
        set(v);
        return true;
    }

    private static bool TextCell(string id, string value, Action<string> set)
    {
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (!ImGui.InputText(id, ref value, 128)) return false;
        set(value);
        return true;
    }
}
