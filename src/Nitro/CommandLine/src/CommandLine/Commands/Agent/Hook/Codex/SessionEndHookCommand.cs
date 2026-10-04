namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>SessionEnd</c> hooks.json event: marks this
/// session's agent row as ended. Payload JSON on stdin, <c>{}</c> on
/// stdout, always.
/// </summary>
internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand() : base("session-end")
    {
        Description = "Adapt Codex CLI's SessionEnd hook: mark this session's agent row as ended.";

        this.SetCodexHookAction(
            "SessionEnd",
            (handler, payload, ct) => handler.HandleSessionEndAsync(payload, ct));
    }
}
