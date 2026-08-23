using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Claude Code's <c>SessionStart</c> hook: upserts this session's
/// presence row. Payload JSON on stdin, <c>{}</c> on stdout, always.
/// </summary>
internal sealed class SessionStartHookCommand : Command
{
    public SessionStartHookCommand() : base("session-start")
    {
        Description = "Adapt Claude Code's SessionStart hook: upsert this session's presence row.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetHookAction((handler, payload, dryRun, ct) => handler.HandleSessionStartAsync(payload, dryRun, ct));
    }
}
