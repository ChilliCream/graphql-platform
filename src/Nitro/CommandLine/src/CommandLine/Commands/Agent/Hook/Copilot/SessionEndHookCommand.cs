using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

/// <summary>
/// Adapts Copilot CLI's <c>sessionEnd</c> hooks-dir event: conditionally
/// deletes this session's presence row. Payload JSON on stdin, <c>{}</c> on
/// stdout, always.
/// </summary>
internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand() : base("session-end")
    {
        Description = "Adapt Copilot CLI's sessionEnd hook: delete this session's presence row.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetCopilotHookAction((handler, payload, dryRun, ct) => handler.HandleSessionEndAsync(payload, dryRun, ct));
    }
}
