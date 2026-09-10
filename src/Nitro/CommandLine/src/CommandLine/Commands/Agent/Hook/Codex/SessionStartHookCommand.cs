namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>SessionStart</c> hooks.json event: upserts this
/// session's presence row. Payload JSON on stdin, <c>{}</c> on stdout,
/// always.
/// </summary>
internal sealed class SessionStartHookCommand : Command
{
    public SessionStartHookCommand() : base("session-start")
    {
        Description = "Adapt Codex CLI's SessionStart hook: upsert this session's presence row.";

        this.SetCodexHookAction(
            "SessionStart",
            (handler, payload, ct) => handler.HandleSessionStartAsync(payload, false, ct));
    }
}
