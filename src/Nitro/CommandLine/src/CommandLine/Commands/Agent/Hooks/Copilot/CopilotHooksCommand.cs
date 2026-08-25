using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot;

/// <summary>
/// Installs, inspects, and removes the project-scoped nitro-mail Copilot CLI
/// extension asset.
/// </summary>
internal sealed class CopilotHooksCommand : Command
{
    public CopilotHooksCommand() : base("copilot")
    {
        Description = "Install, inspect, and remove the nitro-mail Copilot CLI extension asset.";

        Subcommands.Add(new InstallCopilotExtensionCommand());
        Subcommands.Add(new StatusCopilotExtensionCommand());
        Subcommands.Add(new UninstallCopilotExtensionCommand());
    }
}
