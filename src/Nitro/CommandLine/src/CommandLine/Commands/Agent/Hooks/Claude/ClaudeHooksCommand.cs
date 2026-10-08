namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Installs, inspects, and removes this CLI's Claude Code turn-boundary hook
/// entries in <c>settings.json</c>.
/// </summary>
internal sealed class ClaudeHooksCommand : Command
{
    public ClaudeHooksCommand() : base("claude")
    {
        Description = "Install, inspect, and remove Nitro's Claude Code hook entries.";

        Subcommands.Add(new InstallClaudeHooksCommand());
        Subcommands.Add(new StatusClaudeHooksCommand());
        Subcommands.Add(new UninstallClaudeHooksCommand());
    }
}
