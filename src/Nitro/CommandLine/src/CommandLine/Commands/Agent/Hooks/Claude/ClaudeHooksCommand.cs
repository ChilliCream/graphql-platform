using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Installs, inspects, and removes this CLI's turn-boundary hook entries in the
/// <c>settings.json</c> of one harness speaking Claude Code's hook format.
/// </summary>
internal sealed class ClaudeHooksCommand : Command
{
    public ClaudeHooksCommand(ClaudeHooksTemplate template, Option<string> scopeOption) : base(template.CommandName)
    {
        Description =
            $"Install, inspect, and remove Nitro's {AgentHarnessDisplay.Name(template.Harness)} hook entries.";

        Subcommands.Add(new InstallClaudeHooksCommand(template, scopeOption));
        Subcommands.Add(new StatusClaudeHooksCommand(template, scopeOption));
        Subcommands.Add(new UninstallClaudeHooksCommand(template, scopeOption));
    }
}
