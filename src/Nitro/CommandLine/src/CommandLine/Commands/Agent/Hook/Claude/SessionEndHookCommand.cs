namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Claude Code's <c>SessionEnd</c> hook: conditionally deletes this
/// session's presence row. Payload JSON on stdin, <c>{}</c> on stdout,
/// always.
/// </summary>
internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand() : base("session-end")
    {
        Description = "Adapt Claude Code's SessionEnd hook: delete this session's presence row.";

        this.SetHookAction(
            "SessionEnd",
            (handler, payload, ct) => handler.HandleSessionEndAsync(payload, false, ct));
    }
}
