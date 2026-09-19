namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Codex;

/// <summary>
/// Installs, inspects, and removes this CLI's Codex hook entries in
/// <c>~/.codex/hooks.json</c> and notify wiring in <c>~/.codex/config.toml</c>.
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
