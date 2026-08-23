using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

/// <summary>
/// Adapts Copilot CLI's <c>sessionStart</c> hooks-dir event: upserts this
/// session's presence row and returns the initial unread-mail digest, if
/// any. Payload JSON on stdin, <c>{"additionalContext": "..."}</c> or
/// <c>{}</c> on stdout, always.
/// </summary>
internal sealed class SessionStartHookCommand : Command
{
    public SessionStartHookCommand() : base("session-start")
    {
        Description = "Adapt Copilot CLI's sessionStart hook: upsert this session's presence row "
            + "and return the initial unread-mail digest.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetCopilotHookAction((handler, payload, dryRun, ct) => handler.HandleSessionStartAsync(payload, dryRun, ct));
    }
}
