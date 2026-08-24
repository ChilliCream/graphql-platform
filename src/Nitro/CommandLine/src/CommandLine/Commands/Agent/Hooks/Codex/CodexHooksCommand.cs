namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Codex;

/// <summary>
/// Installs, inspects, and removes this CLI's Codex CLI turn-boundary hook
/// entries (<c>~/.codex/hooks.json</c>) and idle-turn gate wiring
/// (<c>notify</c> in <c>~/.codex/config.toml</c>). A sibling command group to
/// <c>agent hooks claude</c> rather than a <c>--harness</c> flag retrofitted
/// onto it: additive only, zero risk to the already-shipped Claude command
/// surface.
/// </summary>
internal sealed class CodexHooksCommand : Command
{
    public CodexHooksCommand() : base("codex")
    {
        Description = "Install, inspect, and remove Nitro's Codex CLI hook and notify entries.";

        Subcommands.Add(new InstallCodexHooksCommand());
        Subcommands.Add(new StatusCodexHooksCommand());
        Subcommands.Add(new UninstallCodexHooksCommand());
    }
}
