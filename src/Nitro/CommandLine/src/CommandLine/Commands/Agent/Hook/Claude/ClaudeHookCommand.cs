namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

internal sealed class ClaudeHookCommand : Command
{
    public ClaudeHookCommand() : base("claude")
    {
        Description = "Adapt Claude Code turn-boundary hook events.";

        Subcommands.Add(new SessionStartHookCommand());
        Subcommands.Add(new UserPromptSubmitHookCommand());
        Subcommands.Add(new StopHookCommand());
        Subcommands.Add(new SessionEndHookCommand());
    }
}
