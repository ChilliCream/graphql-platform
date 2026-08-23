using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Translates harness turn-boundary hook payloads into digest, Stop-gate,
/// and presence behavior. One process per event: payload JSON on stdin,
/// harness-shaped JSON on stdout, always exit 0.
/// </summary>
internal sealed class HookCommand : Command
{
    public HookCommand() : base("hook")
    {
        Description = "Translate harness turn-boundary hook payloads into digest and gate behavior.";

        Subcommands.Add(new ClaudeHookCommand());
    }
}
