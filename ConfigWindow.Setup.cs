using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;

namespace ActionRenamer;

/// <summary>The My plugins page: everything this player had installed, to set it up again on a new PC.</summary>
public sealed partial class ConfigWindow
{
    private void DrawMySetup()
    {
        var pi = Plugin.PluginInterface;
        var installed = pi.InstalledPlugins
            .Where(p => p.IsLoaded || !p.IsOrphaned)
            .Select(p => p.InternalName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var have = MySetup.Plugins.Count(p => installed.Contains(p.InternalName));

        ImGui.TextWrapped("Your saved setup: every plugin you had installed and the repositories they come from. "
            + "Dalamud only lets you add repositories and install plugins yourself, so this page lists them and takes you straight to each one.");
        ImGui.Spacing();
        ImGui.ProgressBar((float)have / MySetup.Plugins.Count, new Vector2(-1, 0), $"{have} of {MySetup.Plugins.Count} plugins installed");

        // Step 1: repositories.
        Section("Step 1 · Add the plugin repositories", $"{MySetup.Repositories.Count} links");
        ImGui.TextWrapped("Open Dalamud settings, go to Experimental, and under Custom Plugin Repositories paste each link into the empty row, "
            + "press the + button, tick Enabled, then Save and Close.");
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, "Open Dalamud settings"))
            pi.OpenDalamudSettingsTo(SettingsOpenKind.Experimental, string.Empty);
        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, "Copy all links"))
            ImGui.SetClipboardText(string.Join(Environment.NewLine, MySetup.Repositories));
        ImGui.Spacing();

        foreach (var url in MySetup.Repositories)
        {
            ImGui.PushID(url);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Copy))
                ImGui.SetClipboardText(url);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy this link");
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(url);
            var count = MySetup.Plugins.Count(p => p.Repo == url);
            ImGui.SameLine();
            ImGui.TextDisabled(count switch { 0 => "(no plugin of yours uses it now)", 1 => "(1 plugin)", _ => $"({count} plugins)" });
            ImGui.PopID();
        }

        // Step 2: plugins.
        Section("Step 2 · Install the plugins", "Find opens the plugin installer on that plugin");
        if (ImGui.BeginTable("plugins", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);
            ImGui.TableSetupColumn("Plugin", ImGuiTableColumnFlags.WidthStretch, 1.2f);
            ImGui.TableSetupColumn("What it does", ImGuiTableColumnFlags.WidthStretch, 3f);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableHeadersRow();

            foreach (var plugin in MySetup.Plugins.OrderBy(p => installed.Contains(p.InternalName)).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                var isInstalled = installed.Contains(plugin.InternalName);
                ImGui.PushID(plugin.InternalName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(isInstalled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudOrange,
                    (isInstalled ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.ExclamationCircle).ToIconString());
                ImGui.PopFont();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(isInstalled ? "Installed" : "Not installed");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(plugin.Name);

                ImGui.TableNextColumn();
                ImGui.TextWrapped(plugin.About);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"From {plugin.Repo}");

                ImGui.TableNextColumn();
                if (!isInstalled && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Search, "Find"))
                    pi.OpenPluginInstallerTo(PluginInstallerOpenKind.AllPlugins, plugin.Name);

                ImGui.PopID();
            }
            ImGui.EndTable();
        }

        // Step 3: mods.
        Section("Step 3 · Penumbra mods");
        ImGui.TextWrapped("Your only Penumbra mod is the Action Renamer icon pack, and this plugin installs it by itself once Penumbra is in. "
            + "Penumbra keeps mods in C:\\FFXIVMods; set the same folder in Penumbra's settings on a new PC.");
    }
}
