using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

namespace ActionRenamer;

/// <summary>The chat window: ask Claude about the game.</summary>
public sealed class AskWindow : Window
{
    private readonly Plugin plugin;
    private string input = string.Empty;
    private int lastCount;

    public AskWindow(Plugin plugin) : base("Ask Claude###ActionRenamerAsk")
    {
        this.plugin = plugin;
        Size = new Vector2(460, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(320, 260), MaximumSize = new Vector2(2000, 2000) };
    }

    /// <summary>Opens the window and asks right away (used by the /ask command).</summary>
    public void AskNow(string question)
    {
        IsOpen = true;
        if (question.Trim().Length > 0)
            plugin.Ask.Ask(question, plugin.PlayerContext());
    }

    public override void Draw()
    {
        var ask = plugin.Ask;
        var messages = ask.Messages;

        ImGui.TextColored(ImGuiColors.DalamudViolet, "Ask about the game");
        ImGui.SameLine();
        ImGui.TextDisabled($"({(plugin.Config.AskModel == "sonnet" ? "Sonnet" : "Haiku")}, your Claude subscription)");
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 50 + ImGui.GetCursorPosX());
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            ask.Clear();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear the chat");

        var inputHeight = ImGui.GetFrameHeightWithSpacing() * 2;
        using (var history = Dalamud.Interface.Utility.Raii.ImRaii.Child("history", new Vector2(0, -inputHeight), true))
        {
            if (history)
            {
                if (messages.Length == 0)
                    ImGui.TextWrapped("Ask anything about FINAL FANTASY XIV: where a quest starts, how to unlock a duty, what an item is for. "
                        + "Answers use your own game data and, if needed, a web search.");
                foreach (var m in messages)
                {
                    ImGui.TextColored(m.FromPlayer ? ImGuiColors.TankBlue : m.IsError ? ImGuiColors.DalamudRed : ImGuiColors.HealerGreen,
                        m.FromPlayer ? "You" : "Claude");
                    ImGui.TextWrapped(m.Text);
                    ImGui.Spacing();
                }
                if (ask.Busy)
                    ImGui.TextColored(ImGuiColors.DalamudGrey, "Thinking" + new string('.', 1 + (int)(ImGui.GetTime() * 2) % 3));
                if (messages.Length != lastCount)
                {
                    ImGui.SetScrollHereY(1f);
                    lastCount = messages.Length;
                }
            }
        }

        ImGui.Spacing();
        var send = false;
        using (Dalamud.Interface.Utility.Raii.ImRaii.Disabled(ask.Busy))
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 70);
            send |= ImGui.InputTextWithHint("##ask", "Type a question and press Enter", ref input, 500, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.SameLine();
            send |= ImGui.Button("Ask", new Vector2(-1, 0));
        }
        if (send && input.Trim().Length > 0 && !ask.Busy)
        {
            ask.Ask(input, plugin.PlayerContext());
            input = string.Empty;
            ImGui.SetKeyboardFocusHere(-1);
        }
    }
}
