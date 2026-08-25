namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension;

/// <summary>
/// Installs, inspects, and removes the project-scoped nitro-mail Copilot CLI
/// extension asset (<c>.github/extensions/nitro-mail/extension.mjs</c>), a sibling to
/// <c>agent hooks copilot install/status/uninstall</c>'s user-scope
/// <c>~/.copilot/hooks/nitro-mail.json</c>. A separate command group, not
/// options on the existing install command: the two artifacts have
/// different scope rules (hooks user-scope only, extension project-scope
/// only) and different overwrite semantics (hooks merges into a shared JSON
/// file, the extension is a single whole-file asset compared by content
/// hash).
/// </summary>
internal sealed class CopilotExtensionCommand : Command
{
    public CopilotExtensionCommand() : base("extension")
    {
        Description = "Install, inspect, and remove the nitro-mail Copilot CLI extension asset.";

        Subcommands.Add(new InstallCopilotExtensionCommand());
        Subcommands.Add(new StatusCopilotExtensionCommand());
        Subcommands.Add(new UninstallCopilotExtensionCommand());
    }
}
