using System;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace ActionRenamer;

/// <summary>Rewrites chat log lines ("You cast Fire.") and floating combat text.</summary>
public sealed class MessageRewriter : IDisposable
{
    private readonly IChatGui chat;
    private readonly IFlyTextGui flyText;
    private readonly Configuration config;
    private readonly Func<RenameEngine> engine;

    public MessageRewriter(IChatGui chat, IFlyTextGui flyText, Configuration config, Func<RenameEngine> engine)
    {
        this.chat = chat;
        this.flyText = flyText;
        this.config = config;
        this.engine = engine;

        chat.ChatMessage += OnChatMessage;
        flyText.FlyTextCreated += OnFlyText;
    }

    private void OnChatMessage(Dalamud.Game.Chat.IHandleableChatMessage message)
    {
        if (!config.Enabled || !config.ApplyToChat || IsPlayerChannel((int)message.LogKind))
            return;
        var text = message.Message;
        if (engine().Apply(text, sentences: true))
            message.Message = text;
    }

    /// <summary>Channels where players type (say, party, tells, linkshells, ...). Their words are left alone.</summary>
    private static bool IsPlayerChannel(int kind) =>
        kind is >= 10 and <= 30 and not 25 and not 26 // Say..Yell, minus non-player gaps
        || kind is 32 or 36 or 37 // cross-world party, PvP team, CWLS1
        || kind is >= 101 and <= 107; // CWLS2-8

    private void OnFlyText(
        ref FlyTextKind kind, ref int val1, ref int val2,
        ref SeString text1, ref SeString text2,
        ref uint color, ref uint icon, ref uint damageTypeIcon,
        ref float yOffset, ref bool handled)
    {
        if (!config.Enabled || !config.ApplyToFlyText)
            return;
        engine().Apply(text1, sentences: true);
        engine().Apply(text2, sentences: true);
    }

    public void Dispose()
    {
        chat.ChatMessage -= OnChatMessage;
        flyText.FlyTextCreated -= OnFlyText;
    }
}
