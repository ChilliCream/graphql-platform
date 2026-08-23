namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

internal sealed class CopilotHookCommand : Command
{
    public CopilotHookCommand() : base("copilot")
    {
        Description = "Adapt GitHub Copilot CLI turn-boundary hook events.";

        Subcommands.Add(new SessionStartHookCommand());
        Subcommands.Add(new UserPromptSubmitHookCommand());
        Subcommands.Add(new SessionEndHookCommand());
    }
}
