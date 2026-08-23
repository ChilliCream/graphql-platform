namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot;

/// <summary>
/// Installs, inspects, and removes this CLI's Copilot CLI turn-boundary hook
/// entries (<c>~/.copilot/hooks/nitro-mail.json</c>). A sibling command
/// group to the existing Claude-only <c>agent hooks install/status/uninstall</c>
/// and to <c>agent hooks codex</c> (perles-net-k3j.12), additive only, zero
/// risk to either already-shipped command surface.
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
