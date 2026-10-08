namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Claude Code's <c>SessionEnd</c> hook: marks this session's agent
/// row as ended. Payload JSON on stdin, <c>{}</c> on stdout, always.
/// </summary>
internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand() : base("session-end")
    {
        Description = "Adapt Claude Code's SessionEnd hook: mark this session's agent row as ended.";

        this.SetHookAction(
            "SessionEnd",
            (handler, payload, ct) => handler.HandleSessionEndAsync(payload, skipSessionFileLookup: false, ct));
    }
}
