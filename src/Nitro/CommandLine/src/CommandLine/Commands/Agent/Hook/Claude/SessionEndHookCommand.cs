using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts the <c>SessionEnd</c> hook of a harness speaking Claude Code's hook
/// contract: marks this session's agent row as ended. Payload JSON on stdin,
/// <c>{}</c> on stdout, always.
/// </summary>
internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand(ClaudeHooksTemplate template) : base("session-end")
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)}'s SessionEnd hook: "
            + "mark this session's agent row as ended.";

        this.SetHookAction(
            template.Harness,
            "SessionEnd",
            (handler, payload, ct) => handler.HandleSessionEndAsync(payload, skipSessionFileLookup: false, ct));
    }
}
