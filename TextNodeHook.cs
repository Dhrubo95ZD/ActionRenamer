using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ActionRenamer;

/// <summary>
/// Hooks AtkTextNode.SetText, the function the game calls whenever any UI text changes.
/// Every string (class names in any window, action names, cast bars) passes through the rules here.
/// Results are cached by content, so repeated text costs one dictionary lookup.
/// </summary>
public sealed unsafe class TextNodeHook : IDisposable
{
    private delegate void SetTextDelegate(AtkTextNode* node, byte* text);

    private readonly Hook<SetTextDelegate> hook;
    private readonly Configuration config;
    private readonly Func<RenameEngine> engine;
    private readonly IPluginLog log;

    // Original bytes -> rewritten bytes (null terminated), or null when the rules leave the text alone.
    private readonly Dictionary<string, byte[]?> cache = new();

    public TextNodeHook(IGameInteropProvider interop, IPluginLog log, Configuration config, Func<RenameEngine> engine)
    {
        this.log = log;
        this.config = config;
        this.engine = engine;
        hook = interop.HookFromAddress<SetTextDelegate>((nint)AtkTextNode.MemberFunctionPointers.SetText, Detour);
        hook.Enable();
    }

    /// <summary>Forget cached results (call after rules change).</summary>
    public void ResetCache() => cache.Clear();

    private bool firstCallLogged;

    private void Detour(AtkTextNode* node, byte* text)
    {
        if (!firstCallLogged)
        {
            firstCallLogged = true;
            log.Info("SetText hook is receiving calls");
        }

        byte[]? replacement = null;
        try
        {
            if (config.Enabled && config.ApplyToUiText && text != null && !engine().IsEmpty)
                replacement = Rewrite(text);
        }
        catch (Exception e)
        {
            log.Error(e, "SetText rewrite failed");
        }

        if (replacement == null)
        {
            hook.Original(node, text);
            return;
        }

        fixed (byte* ptr = replacement)
            hook.Original(node, ptr);
    }

    private byte[]? Rewrite(byte* text)
    {
        var span = new ReadOnlySpan<byte>(text, int.MaxValue);
        span = span[..span.IndexOf((byte)0)];
        if (span.IsEmpty)
            return null;

        // Latin1 maps bytes 1:1, so it is a cheap exact key for the raw SeString bytes.
        var key = System.Text.Encoding.Latin1.GetString(span);
        if (cache.TryGetValue(key, out var cached))
            return cached;

        byte[]? result = null;
        var seString = SeString.Parse(span);
        if (engine().Apply(seString))
        {
            var encoded = seString.Encode();
            result = new byte[encoded.Length + 1];
            encoded.CopyTo(result, 0);
            if (config.LogChanges)
                log.Info($"SetText changed: \"{SeString.Parse(span).TextValue}\" -> \"{seString.TextValue}\"");
        }
        else if (config.LogChanges && engine().Mentions(seString.TextValue))
        {
            log.Info($"SetText saw but did not change: \"{seString.TextValue}\" bytes={Convert.ToHexString(span[..Math.Min(span.Length, 48)])}");
        }

        if (cache.Count > 20000)
            cache.Clear();
        cache[key] = result;
        return result;
    }

    public void Dispose() => hook.Dispose();
}
