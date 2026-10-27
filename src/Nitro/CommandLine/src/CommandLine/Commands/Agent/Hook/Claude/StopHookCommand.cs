using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts the <c>Stop</c> hook of a harness speaking Claude Code's hook contract:
/// blocks the turn from ending while unread mail not yet delivered on the gate
/// channel exists, honoring reentrancy and the per-turn block budget. Payload JSON
/// on stdin, <c>{decision, reason}</c> or <c>{}</c> on stdout, always.
/// </summary>
internal sealed class StopHookCommand : Command
{
    public StopHookCommand(ClaudeHooksTemplate template) : base("stop")
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)}'s Stop hook: "
            + "block the turn while unread mail is undelivered.";

        this.SetHookAction(
            template.Harness,
            "Stop",
            (handler, payload, ct) => handler.HandleStopAsync(payload, skipSessionFileLookup: false, ct));
    }
}
