namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot;

/// <summary>
/// Installs, inspects, and removes this CLI's Copilot CLI turn-boundary hook
/// entries (<c>~/.copilot/hooks/nitro-mail.json</c>). A sibling command
/// group to <c>agent hooks claude</c> and <c>agent hooks codex</c>, additive
/// only, zero risk to either already-shipped command surface.
/// </summary>
internal sealed class CopilotHooksCommand : Command
{
    public CopilotHooksCommand() : base("copilot")
    {
        Description = "Install, inspect, and remove Nitro's Copilot CLI hook entries.";

        Subcommands.Add(new InstallCopilotHooksCommand());
        Subcommands.Add(new StatusCopilotHooksCommand());
        Subcommands.Add(new UninstallCopilotHooksCommand());
        Subcommands.Add(new Extension.CopilotExtensionCommand());
    }
}
