namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Installs, inspects, and removes this CLI's turn-boundary hook entries,
/// one nested group per harness (<c>claude</c>, <c>codex</c>,
/// <c>copilot</c>). Distinct from <c>nitro agent hook</c> (singular): that
/// group IS the hook adapters each harness invokes; this group manages the
/// config entries that wire a harness up to them.
/// </summary>
internal sealed class HooksCommand : Command
{
    public HooksCommand() : base("hooks")
    {
        Description = "Install, inspect, and remove Nitro's turn-boundary hook entries per harness.";

        Subcommands.Add(new Claude.ClaudeHooksCommand());
        Subcommands.Add(new Codex.CodexHooksCommand());
        Subcommands.Add(new Copilot.CopilotHooksCommand());
    }
}
