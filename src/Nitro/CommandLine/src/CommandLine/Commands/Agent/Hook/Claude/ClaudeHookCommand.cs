using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Groups the turn-boundary hook adapters of one harness speaking Claude Code's hook contract.
/// </summary>
internal sealed class ClaudeHookCommand : Command
{
    public ClaudeHookCommand(ClaudeHooksTemplate template) : base(template.CommandName)
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)} turn-boundary hook events.";

        if (template.Events.Contains(ClaudeHooksTemplate.AgentNamingEvent))
        {
            Subcommands.Add(new AgentNamingHookCommand(template));
        }

        Subcommands.Add(new SessionStartHookCommand(template));
        Subcommands.Add(new UserPromptSubmitHookCommand(template));
        Subcommands.Add(new StopHookCommand(template));
        Subcommands.Add(new NotificationHookCommand(template));
        Subcommands.Add(new SessionEndHookCommand(template));
    }
}
