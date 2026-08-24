namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Installs, inspects, and removes this CLI's Claude Code turn-boundary hook
/// entries in <c>settings.json</c>. Distinct from <c>nitro agent hook</c>
/// (singular): that group IS the hook adapters Claude Code invokes; this
/// group manages the config entries that wire Claude Code up to them. A
/// sibling command group to <c>agent hooks codex</c> and
/// <c>agent hooks copilot</c>; the bare <c>agent hooks install/status/uninstall</c>
/// verbs remain as deprecated aliases for this group's verbs.
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
