using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ActionRenamer;

/// <summary>
/// Action tooltip (ActionDetail): swaps in a custom description for the hovered action, or blanks it.
/// The description is the longest text node in the tooltip; that holds for every action tooltip.
/// </summary>
public sealed unsafe class TooltipRewriter : IDisposable
{
    private const string Addon = "ActionDetail";

    private readonly IAddonLifecycle lifecycle;
    private readonly IGameGui gameGui;
    private readonly IPluginLog log;
    private readonly Configuration config;

    public TooltipRewriter(IAddonLifecycle lifecycle, IGameGui gameGui, IPluginLog log, Configuration config)
    {
        this.lifecycle = lifecycle;
        this.gameGui = gameGui;
        this.log = log;
        this.config = config;
        lifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, Addon, OnUpdate);
        lifecycle.RegisterListener(AddonEvent.PostRefresh, Addon, OnUpdate);
        lifecycle.RegisterListener(AddonEvent.PreDraw, Addon, OnUpdate);
    }

    private void OnUpdate(AddonEvent type, AddonArgs args)
    {
        if (!config.Enabled)
            return;
        try
        {
            var hovered = gameGui.HoveredAction;
            var isTrait = hovered.DetailKind.ToString().Contains("Trait", StringComparison.OrdinalIgnoreCase);
            uint Key(uint id) => isTrait ? Configuration.TraitKey(id) : id;
            ActionOverride? ov = null;
            if (!config.Overrides.TryGetValue(Key(hovered.ActionId), out ov))
                config.Overrides.TryGetValue(Key(hovered.BaseActionId), out ov);

            var addonForCapture = (AtkUnitBase*)args.Addon.Address;
            if (addonForCapture != null)
                Capture(addonForCapture, Key(hovered.ActionId), ov);

            string? newDescription = null;
            if (ov != null && ov.HideDescription) newDescription = string.Empty;
            else if (ov != null && !string.IsNullOrWhiteSpace(ov.Description)) newDescription = ov.Description;
            else if (config.HideAllDescriptions) newDescription = string.Empty;
            if (newDescription == null)
                return;

            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null)
                return;

            if (config.LogChanges && hovered.ActionId != lastLoggedAction)
            {
                lastLoggedAction = hovered.ActionId;
                DumpTexts(&addon->UldManager, 0, hovered.ActionId);
            }

            // Descriptions are long; names, level and range lines are short. Never touch a short node,
            // so a tooltip without a description cannot lose its name.
            var description = FindLongestText(&addon->UldManager, 0);
            if (description == null || TextOf(description).Length < MinDescriptionLength)
                return;

            // An empty node collapses the tooltip layout; a single space keeps it intact.
            if (newDescription.Length == 0)
                newDescription = " ";

            var current = TextOf(description);
            if (current == newDescription)
                return;

            var bytes = Build(newDescription).Encode();
            var buffer = new byte[bytes.Length + 1];
            bytes.CopyTo(buffer, 0);
            fixed (byte* ptr = buffer)
                description->SetText(ptr);

            if (config.LogChanges && type != AddonEvent.PreDraw)
                log.Info($"Tooltip description for action {hovered.ActionId} replaced");
        }
        catch (Exception e)
        {
            log.Error(e, "Tooltip rewrite failed");
        }
    }

    private const int MinDescriptionLength = 25;

    /// <summary>Game's own description per override key, captured on hover; feeds the "Game text" button.</summary>
    public static readonly System.Collections.Generic.Dictionary<uint, string> GameDescriptions = new();

    private static void Capture(AtkUnitBase* addon, uint key, ActionOverride? ov)
    {
        var node = FindLongestText(&addon->UldManager, 0);
        if (node == null)
            return;
        var text = TextOf(node);
        // Skip our own text so the game's original stays stored.
        if (text.Length < MinDescriptionLength || (ov != null && text == ov.Description.Replace("\r", string.Empty)))
            return;
        GameDescriptions[key] = text;
    }

    /// <summary>Plain text to SeString; line breaks become game new-line payloads.</summary>
    private static SeString Build(string text)
    {
        var builder = new SeStringBuilder();
        var lines = text.Replace("\r", string.Empty).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) builder.Add(Dalamud.Game.Text.SeStringHandling.Payloads.NewLinePayload.Payload);
            if (lines[i].Length > 0) builder.AddText(lines[i]);
        }
        return builder.Build();
    }
    private uint lastLoggedAction = uint.MaxValue;

    private static ReadOnlySpan<byte> Bytes(AtkTextNode* node)
    {
        var span = node->NodeText.AsSpan();
        var nul = span.IndexOf((byte)0);
        return nul >= 0 ? span[..nul] : span;
    }

    private static string TextOf(AtkTextNode* node) => SeString.Parse(Bytes(node)).TextValue;

    private void DumpTexts(AtkUldManager* uld, int depth, uint actionId)
    {
        if (uld == null || uld->NodeList == null || depth > 6)
            return;
        for (var i = 0; i < uld->NodeListCount; i++)
        {
            var node = uld->NodeList[i];
            if (node == null) continue;
            if (node->Type == NodeType.Text)
                log.Info($"[ActionDetail {actionId}] depth {depth} node {node->NodeId} visible={node->IsVisible()} \"{TextOf((AtkTextNode*)node)}\"");
            else if ((int)node->Type >= 1000 && ((AtkComponentNode*)node)->Component != null)
                DumpTexts(&((AtkComponentNode*)node)->Component->UldManager, depth + 1, actionId);
        }
    }

    private static AtkTextNode* FindLongestText(AtkUldManager* uld, int depth)
    {
        if (uld == null || uld->NodeList == null || depth > 6)
            return null;

        AtkTextNode* best = null;
        var bestLength = 0;
        for (var i = 0; i < uld->NodeListCount; i++)
        {
            var node = uld->NodeList[i];
            if (node == null)
                continue;

            AtkTextNode* candidate = null;
            if (node->Type == NodeType.Text)
                candidate = (AtkTextNode*)node;
            else if ((int)node->Type >= 1000 && ((AtkComponentNode*)node)->Component != null)
                candidate = FindLongestText(&((AtkComponentNode*)node)->Component->UldManager, depth + 1);

            if (candidate == null)
                continue;
            var length = Bytes(candidate).Length;
            if (length > bestLength)
            {
                best = candidate;
                bestLength = length;
            }
        }
        return best;
    }

    public void Dispose() => lifecycle.UnregisterListener(OnUpdate);
}
