using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Venti's <c>AgentNaming</c> hook: registers a new main session and answers with its
/// actor name as the agent name. Payload JSON on stdin; <c>{}</c> on stdout for any other agent.
/// </summary>
internal sealed class AgentNamingHookCommand : Command
{
    public AgentNamingHookCommand(ClaudeHooksTemplate template) : base("agent-naming")
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)}'s AgentNaming hook: "
            + "name a new main session after its actor.";

        this.SetHookAction(
            template.Harness,
            ClaudeHooksTemplate.AgentNamingEvent,
            (handler, payload, ct) => handler.HandleAgentNamingAsync(payload, skipSessionFileLookup: false, ct));
    }
}
