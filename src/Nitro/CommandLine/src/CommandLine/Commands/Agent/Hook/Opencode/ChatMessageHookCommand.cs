namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Adapts the opencode shim's <c>chat.message</c> event: refreshes this
/// session's heartbeat and returns the actor announcement and unread-mail
/// digest as chat parts. Payload JSON on stdin, <c>{parts}</c> or
/// <c>{}</c> on stdout, always.
/// </summary>
internal sealed class ChatMessageHookCommand : Command
{
    public ChatMessageHookCommand() : base("chat-message")
    {
        Description = "Adapt opencode's chat.message event: append the actor announcement and mail digest.";

        this.SetOpencodeHookAction(
            (handler, payload, ct) => handler.HandleChatMessageAsync(payload, false, ct));
    }
}
