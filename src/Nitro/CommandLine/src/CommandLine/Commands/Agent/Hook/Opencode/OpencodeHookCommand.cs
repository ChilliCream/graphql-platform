namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Adapts the generated opencode plugin shim's turn-boundary events.
/// </summary>
internal sealed class OpencodeHookCommand : Command
{
    public OpencodeHookCommand() : base("opencode")
    {
        Description = "Adapt opencode plugin shim turn-boundary events.";

        Subcommands.Add(new SessionCreatedHookCommand());
        Subcommands.Add(new ChatMessageHookCommand());
        Subcommands.Add(new SessionIdleHookCommand());
        Subcommands.Add(new SessionDeletedHookCommand());
    }
}
