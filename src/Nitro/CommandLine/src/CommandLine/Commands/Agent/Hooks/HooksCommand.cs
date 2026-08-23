namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Installs, inspects, and removes this CLI's Claude Code turn-boundary hook
/// entries in <c>settings.json</c>. Distinct from <c>nitro agent hook</c>
/// (singular): that group IS the hook adapters Claude Code invokes; this
/// group manages the config entries that wire Claude Code up to them. The
/// top-level verbs (<c>install</c>/<c>status</c>/<c>uninstall</c>) stay
/// Claude Code-only, unchanged since they first shipped; Codex CLI is a
/// separate <c>codex</c> subcommand rather than a <c>--harness</c> flag
/// retrofitted onto them (perles-net-k3j.12: additive only, zero risk to the
/// existing Claude command surface).
/// </summary>
internal sealed class HooksCommand : Command
{
    public HooksCommand() : base("hooks")
    {
        Description = "Install, inspect, and remove Nitro's Claude Code hook entries.";

        Subcommands.Add(new InstallHooksCommand());
        Subcommands.Add(new StatusHooksCommand());
        Subcommands.Add(new UninstallHooksCommand());
        Subcommands.Add(new Codex.CodexHooksCommand());
        Subcommands.Add(new Copilot.CopilotHooksCommand());
    }
}
