namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>SessionEnd</c> hooks.json event: conditionally
/// deletes this session's presence row. Payload JSON on stdin, <c>{}</c> on
/// stdout, always.
/// </summary>
internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand() : base("session-end")
    {
        Description = "Adapt Codex CLI's SessionEnd hook: delete this session's presence row.";

        this.SetCodexHookAction(
            "SessionEnd",
            (handler, payload, ct) => handler.HandleSessionEndAsync(payload, false, ct));
    }
}
