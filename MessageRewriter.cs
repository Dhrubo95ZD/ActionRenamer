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
        if (!config.Enabled || !config.ApplyToChat)
            return;
        var text = message.Message;
        if (engine().Apply(text))
            message.Message = text;
    }

    private void OnFlyText(
        ref FlyTextKind kind, ref int val1, ref int val2,
        ref SeString text1, ref SeString text2,
        ref uint color, ref uint icon, ref uint damageTypeIcon,
        ref float yOffset, ref bool handled)
    {
        if (!config.Enabled || !config.ApplyToFlyText)
            return;
        engine().Apply(text1);
        engine().Apply(text2);
    }

    public void Dispose()
    {
        chat.ChatMessage -= OnChatMessage;
        flyText.FlyTextCreated -= OnFlyText;
    }
}
