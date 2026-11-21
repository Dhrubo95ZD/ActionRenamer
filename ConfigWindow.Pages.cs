using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace ActionRenamer;

/// <summary>The General, Duty helpers and Auto-queue pages of the settings window.</summary>
public sealed partial class ConfigWindow
{
    private string dutySearch = string.Empty;

    // ---- shared look -------------------------------------------------------------------------------

    /// <summary>A coloured section title with a rule under it.</summary>
    private static void Section(string title, string? subtitle = null)
    {
        ImGui.Spacing();
        ImGui.TextColored(ImGuiColors.DalamudViolet, title);
        if (subtitle != null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(subtitle);
        }
        ImGui.Separator();
        ImGui.Spacing();
    }

    /// <summary>A switch with its label and an optional (?) explanation. Returns true when flipped.</summary>
    private static bool Toggle(string label, Func<bool> get, Action<bool> set, string? help = null)
    {
        var v = get();
        var changed = ImGuiComponents.ToggleButton($"##{label}", ref v);
        if (changed)
            set(v);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        if (help != null)
        {
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(help);
        }
        return changed;
    }

    private static void StatusLine(string text)
    {
        ImGui.Indent();
        ImGui.TextColored(ImGuiColors.DalamudGrey, text);
        ImGui.Unindent();
    }

    // ---- General -----------------------------------------------------------------------------------

    /// <summary>Renaming and icon switches. Returns true when the rename engine must be rebuilt.</summary>
    private bool DrawGeneral(Configuration cfg)
    {
        var dirty = false;

        Section("Renaming", "sci-fi names for skills, jobs and status effects");
        dirty |= Toggle("Renaming on", () => cfg.Enabled, v => cfg.Enabled = v, "The master switch for all renaming. Duty helpers keep working when this is off.");
        using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(!cfg.Enabled))
        {
            dirty |= Toggle("Game windows and tooltips", () => cfg.ApplyToUiText, v => cfg.ApplyToUiText = v);
            dirty |= Toggle("Chat", () => cfg.ApplyToChat, v => cfg.ApplyToChat = v);
            dirty |= Toggle("Damage and heal numbers (fly text)", () => cfg.ApplyToFlyText, v => cfg.ApplyToFlyText = v);
            dirty |= Toggle("Sci-fi theme for skills, traits and job names", () => cfg.SkillThemeEnabled, v => cfg.SkillThemeEnabled = v);
            dirty |= Toggle("Sci-fi theme for status effects", () => cfg.StatusThemeEnabled, v => cfg.StatusThemeEnabled = v);
            dirty |= Toggle("Hide every skill description", () => cfg.HideAllDescriptions, v => cfg.HideAllDescriptions = v,
                "Blanks the description of every skill tooltip that has no custom description of its own.");
        }

        Section("Icons", "sci-fi icon pack through Penumbra");
        dirty |= Toggle("Install the icon pack in Penumbra", () => cfg.AutoInstallIcons, v => cfg.AutoInstallIcons = v,
            "Installs and enables the bundled icon mod. Log out and back in to see new icons.");
        ImGui.Indent();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Redo, "Reinstall icons"))
            plugin.Icons.Reinstall();
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudGrey, plugin.Icons.Status);
        ImGui.Unindent();

        Section("Ask Claude", "a chat about the game, through Claude Code on this PC");
        dirty |= Toggle("Ask Claude chat and /ask command", () => cfg.AskEnabled, v => cfg.AskEnabled = v,
            "Uses the Claude Code app installed on this PC and your Claude subscription. No API key. Each question counts toward your plan's usage. "
            + "Claude only gets your question, your job, level and zone, and matching game data.");
        using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(!cfg.AskEnabled))
        {
            ImGui.Indent();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Model");
            ImGui.SameLine();
            if (ImGui.RadioButton("Haiku (cheaper)", cfg.AskModel != "sonnet")) { cfg.AskModel = "haiku"; dirty = true; }
            ImGui.SameLine();
            if (ImGui.RadioButton("Sonnet (smarter)", cfg.AskModel == "sonnet")) { cfg.AskModel = "sonnet"; dirty = true; }
            ImGui.Unindent();
            dirty |= Toggle("\"Ask Claude\" button in the status bar", () => cfg.AskStatusBar, v => cfg.AskStatusBar = v,
                "A clickable entry at the bottom right of the screen, next to the other plugins' status. Click it to open or close the chat.");
            dirty |= Toggle("Search the web when the game data is not enough", () => cfg.AskWebSearch, v => cfg.AskWebSearch = v,
                "Slower, but finds guides and strategies that are not in the game files.");
            ImGui.Indent();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Comments, "Open the chat"))
                plugin.AskWindow.IsOpen = true;
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudGrey, "or type /askclaude <question> (also /ask if no other plugin uses it)");
            var path = cfg.ClaudePath;
            ImGui.SetNextItemWidth(360);
            if (ImGui.InputTextWithHint("Claude Code path", plugin.Ask.ClaudePath, ref path, 260))
                cfg.ClaudePath = path;
            dirty |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.Unindent();
        }

        Section("Troubleshooting");
        dirty |= Toggle("Write each rename to the log", () => cfg.LogChanges, v => cfg.LogChanges = v, "For finding text that is not renamed. Open the log with /xllog.");

        return dirty;
    }

    // ---- Duty helpers ------------------------------------------------------------------------------

    /// <summary>Duty switches save on their own; they do not need the rename engine rebuilt.</summary>
    private void DrawDutyHelpers(Configuration cfg)
    {
        var dirty = false;

        Section("Answer for me", "presses the button so you do not have to");
        dirty |= Toggle("Commence when a duty pops", () => cfg.AutoCommenceDuty, v => cfg.AutoCommenceDuty = v);
        dirty |= Toggle("Yes to ready checks", () => cfg.AutoReadyCheck, v => cfg.AutoReadyCheck = v);
        dirty |= Toggle("Yes to \"Move immediately to sealed area?\"", () => cfg.AutoSealedArea, v => cfg.AutoSealedArea = v);
        dirty |= Toggle("Accept raises when I am knocked out", () => cfg.AutoAcceptRaise, v => cfg.AutoAcceptRaise = v,
            "Answers Yes to \"Accept Raise from ...?\" as soon as someone raises you.");

        Section("Follow the tank", "needs Boss Mod");
        dirty |= Toggle("Boss Mod follows the tank in duties", () => cfg.AutoFollowTank, v => cfg.AutoFollowTank = v,
            "Between fights you walk after the tank. During boss fights Boss Mod dodges instead. Nothing happens when you are the tank.");
        using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(!cfg.AutoFollowTank))
        {
            ImGui.Indent();
            var followDistance = cfg.FollowDistance;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Distance to the tank (yalms)", ref followDistance, 3, 25))
                cfg.FollowDistance = followDistance;
            dirty |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.Unindent();
        }
        StatusLine(plugin.Follow.Status);

        Section("Leave when done");
        dirty |= Toggle("Leave the duty once it is finished and the loot is done", () => cfg.AutoLeaveDuty, v => cfg.AutoLeaveDuty = v,
            "Waits for the coffers (two minutes at most) and for all loot to be rolled for. Never leaves in combat or a cutscene. Duty Finder content only. Does not roll for you.");
        using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(!cfg.AutoLeaveDuty))
        {
            dirty |= Toggle("Leave as soon as my own rolls are in", () => cfg.AutoLeaveAfterOwnRolls, v => cfg.AutoLeaveAfterOwnRolls = v,
                "Off: wait until every item is handed out. On: do not wait for the rest of the party to roll.");
            ImGui.Indent();
            var delay = cfg.AutoLeaveDelaySeconds;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Seconds to wait before leaving", ref delay, 1, 60))
                cfg.AutoLeaveDelaySeconds = delay;
            dirty |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.Unindent();
        }
        StatusLine(plugin.Duty.Status);
        StatusLine("To stay in a finished duty once, type /actionrenamer stay.");

        if (dirty)
            Plugin.PluginInterface.SavePluginConfig(cfg);
    }

    // ---- Auto-queue --------------------------------------------------------------------------------

    private void DrawAutoQueue(Configuration cfg)
    {
        var dirty = false;
        var queue = plugin.Queue;
        var catalog = plugin.Duties;

        var avail = ImGui.GetContentRegionAvail();
        var rightWidth = Math.Clamp(avail.X * 0.4f, 260f, 420f);

        // Left: every queueable duty, grouped like the Duty Finder.
        using (var left = Dalamud.Interface.Utility.Raii.ImRaii.Child("duties", new Vector2(avail.X - rightWidth - 8, 0), true))
        {
            if (left)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##dutysearch", "Search duties...", ref dutySearch, 64);
                ImGui.Spacing();

                var searching = dutySearch.Length > 0;
                foreach (var kind in DutyCatalog.KindOrder)
                {
                    var inKind = catalog.All.Where(d => d.Kind == kind && (!searching || d.Name.Contains(dutySearch, StringComparison.OrdinalIgnoreCase))).ToList();
                    if (inKind.Count == 0)
                        continue;
                    if (searching)
                        ImGui.SetNextItemOpen(true);
                    if (!ImGui.CollapsingHeader($"{KindLabel(kind)}  ({inKind.Count})###kind{kind}"))
                        continue;

                    foreach (var group in inKind.GroupBy(d => d.Expansion).OrderBy(g => DutyCatalog.ExpansionRank(g.Key)))
                    {
                        var expansion = group.Key.Length > 0 ? group.Key : "Other";
                        if (searching)
                            ImGui.SetNextItemOpen(true);
                        if (!ImGui.TreeNode($"{expansion}###exp{kind}{expansion}"))
                            continue;
                        foreach (var duty in group)
                            dirty |= DutyRow(cfg, duty);
                        ImGui.TreePop();
                    }
                }
            }
        }

        ImGui.SameLine();

        // Right: the chosen duty, run count and start/stop.
        using (var right = Dalamud.Interface.Utility.Raii.ImRaii.Child("queue", new Vector2(rightWidth, 0), true))
        {
            if (right)
            {
                Section("Auto-queue");
                catalog.ById.TryGetValue(cfg.QueueDutyId, out var chosen);
                if (chosen == null)
                {
                    ImGui.TextWrapped("Pick a duty on the left.");
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.ParsedGold, chosen.Name);
                    ImGui.TextDisabled($"{KindLabel(chosen.Kind)}{(chosen.Expansion.Length > 0 ? $" · {chosen.Expansion}" : "")} · level {chosen.Level}{(chosen.ItemLevel > 0 ? $" · item level {chosen.ItemLevel}" : "")}");
                }
                ImGui.Spacing();

                using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(queue.Running))
                {
                    var runs = cfg.QueueRuns;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.InputInt("Runs", ref runs, 1, 5))
                    {
                        cfg.QueueRuns = Math.Clamp(runs, 1, 999);
                        dirty = true;
                    }
                    var wait = cfg.QueueDelaySeconds;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.SliderInt("Seconds between runs", ref wait, 2, 60))
                        cfg.QueueDelaySeconds = wait;
                    dirty |= ImGui.IsItemDeactivatedAfterEdit();
                    dirty |= Toggle("Unsynced (Unrestricted Party)", () => cfg.QueueUnsynced, v => cfg.QueueUnsynced = v,
                        "Only for a full premade party. Leave off to be matched with other players.");
                }

                ImGui.Spacing();
                if (queue.Running)
                {
                    if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Stop, "Stop", ImGuiColors.DalamudRed))
                        queue.Stop();
                    ImGui.Spacing();
                    var fraction = queue.RunsWanted == 0 ? 0f : (float)queue.RunsDone / queue.RunsWanted;
                    ImGui.ProgressBar(fraction, new Vector2(-1, 0), $"{queue.RunsDone} / {queue.RunsWanted} runs");
                }
                else
                {
                    using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(chosen == null))
                    {
                        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Play, "Start"))
                            queue.Start();
                    }
                }
                ImGui.Spacing();
                ImGui.TextWrapped(queue.Status);

                // Point out switches the loop relies on.
                if (!cfg.AutoCommenceDuty || !cfg.AutoLeaveDuty)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(ImGuiColors.DalamudOrange,
                        !cfg.AutoCommenceDuty && !cfg.AutoLeaveDuty ? "Commence and auto-leave are off in Duty helpers. Each run waits for you to press Commence and to leave."
                        : !cfg.AutoCommenceDuty ? "Commence is off in Duty helpers. Each run waits for you to press Commence."
                        : "Auto-leave is off in Duty helpers. Each run waits for you to leave.");
                }
                ImGui.Spacing();
                ImGui.TextDisabled("Queue as the party leader, or alone.");
            }
        }

        if (dirty)
            Plugin.PluginInterface.SavePluginConfig(cfg);
    }

    /// <summary>One selectable duty. Locked duties are greyed out.</summary>
    private static bool DutyRow(Configuration cfg, DutyEntry duty)
    {
        var locked = duty.InstanceContentId != 0 && !UIState.IsInstanceContentUnlocked(duty.InstanceContentId);
        var label = $"{duty.Name}##{duty.CfcId}";

        bool clicked;
        using (Dalamud.Interface.Utility.Raii.ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3, locked))
            clicked = ImGui.Selectable(label, cfg.QueueDutyId == duty.CfcId, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X - 90, 0));
        if (locked && ImGui.IsItemHovered())
            ImGui.SetTooltip("You have not unlocked this duty yet.");
        ImGui.SameLine();
        ImGui.TextDisabled(duty.ItemLevel > 0 ? $"Lv{duty.Level} i{duty.ItemLevel}" : $"Lv{duty.Level}");

        if (!clicked)
            return false;
        cfg.QueueDutyId = duty.CfcId;
        return true;
    }

    private static string KindLabel(string kind) => kind switch
    {
        "High-end Trials" => "Extreme & Unreal Trials",
        _ => kind,
    };
}
