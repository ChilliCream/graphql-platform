using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts the <c>SessionStart</c> hook of a harness speaking Claude Code's hook
/// contract: upserts this session's presence row. Payload JSON on stdin,
/// <c>{}</c> on stdout, always.
/// </summary>
internal sealed class SessionStartHookCommand : Command
{
    public SessionStartHookCommand(ClaudeHooksTemplate template) : base("session-start")
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)}'s SessionStart hook: "
            + "upsert this session's presence row.";

        this.SetHookAction(
            template.Harness,
            "SessionStart",
            (handler, payload, ct) => handler.HandleSessionStartAsync(payload, skipSessionFileLookup: false, ct));
    }
}
