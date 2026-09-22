namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Adapts the opencode shim's <c>session.deleted</c> event: removes this
/// session's presence row. Payload JSON on stdin, <c>{}</c> on stdout,
/// always.
/// </summary>
internal sealed class SessionDeletedHookCommand : Command
{
    public SessionDeletedHookCommand() : base("session-deleted")
    {
        Description = "Adapt opencode's session.deleted event: remove this session's presence row.";

        this.SetOpencodeHookAction(
            (handler, payload, ct) => handler.HandleSessionDeletedAsync(payload, false, ct));
    }
}
