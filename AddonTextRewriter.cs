using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ActionRenamer;

/// <summary>
/// Second line of defence next to <see cref="TextNodeHook"/>: before each draw, walks the text nodes of
/// windows that list skills and rewrites any text the hook missed (some windows fill text without SetText).
/// A per-node cache skips text it already handled, so steady state costs a byte compare per node.
/// </summary>
public sealed unsafe class AddonTextRewriter : IDisposable
{
    private static readonly string[] Addons =
    {
        "ActionMenu", "ActionMenuReplaceList", "ActionMenuActionSetting", "ActionDetail", "_CastBar", "_TargetInfoCastBar",
    };

    private readonly IAddonLifecycle lifecycle;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly Func<RenameEngine> engine;
    private readonly Dictionary<nint, byte[]> lastSeen = new();
    private readonly HashSet<string> loggedTexts = new();

    public AddonTextRewriter(IAddonLifecycle lifecycle, IPluginLog log, Configuration config, Func<RenameEngine> engine)
    {
        this.lifecycle = lifecycle;
        this.log = log;
        this.config = config;
        this.engine = engine;
        lifecycle.RegisterListener(AddonEvent.PreDraw, Addons, OnPreDraw);
        lifecycle.RegisterListener(AddonEvent.PreFinalize, Addons, OnFinalize);
    }

    /// <summary>Forget cached nodes so every node is re-checked (call after rules change).</summary>
    public void ResetCache()
    {
        lastSeen.Clear();
        loggedTexts.Clear();
    }

    private void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        if (!config.Enabled || !config.ApplyToUiText || engine().IsEmpty)
            return;
        try
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null || !addon->IsVisible)
                return;
            Walk(args.AddonName, &addon->UldManager, 0);
        }
        catch (Exception e)
        {
            log.Error(e, "Window text rewrite failed");
        }
    }

    private void OnFinalize(AddonEvent type, AddonArgs args) => lastSeen.Clear();

    private void Walk(string addonName, AtkUldManager* uld, int depth)
    {
        if (uld == null || uld->NodeList == null || depth > 8)
            return;

        for (var i = 0; i < uld->NodeListCount; i++)
        {
            var node = uld->NodeList[i];
            if (node == null)
                continue;

            if (node->Type == NodeType.Text)
            {
                Rewrite(addonName, (AtkTextNode*)node);
            }
            else if ((int)node->Type >= 1000)
            {
                var component = ((AtkComponentNode*)node)->Component;
                if (component != null)
                    Walk(addonName, &component->UldManager, depth + 1);
            }
        }
    }

    private void Rewrite(string addonName, AtkTextNode* textNode)
    {
        var current = textNode->NodeText.AsSpan();
        var nul = current.IndexOf((byte)0);
        if (nul >= 0) current = current[..nul];
        if (current.IsEmpty)
            return;

        var key = (nint)textNode;
        if (lastSeen.TryGetValue(key, out var seen) && current.SequenceEqual(seen))
            return;

        var seString = SeString.Parse(current);
        var before = seString.TextValue;
        if (!engine().Apply(seString))
        {
            if (config.LogChanges && engine().Mentions(before) && loggedTexts.Add(before))
                log.Info($"[{addonName}] node saw but did not change: \"{before}\" bytes={Convert.ToHexString(current[..Math.Min(current.Length, 48)])}");
            lastSeen[key] = current.ToArray();
            return;
        }

        var encoded = seString.Encode();
        var buffer = new byte[encoded.Length + 1];
        encoded.CopyTo(buffer, 0);
        fixed (byte* ptr = buffer)
            textNode->SetText(ptr);
        lastSeen[key] = encoded;

        if (config.LogChanges && loggedTexts.Add(before))
            log.Info($"[{addonName}] node changed: \"{before}\" -> \"{seString.TextValue}\"");
    }

    public void Dispose() => lifecycle.UnregisterListener(OnPreDraw, OnFinalize);
}
