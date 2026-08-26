namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

internal sealed class CopilotHookCommand : Command
{
    public CopilotHookCommand() : base("copilot")
    {
        Description = "Adapt Copilot CLI lifecycle events.";

        Subcommands.Add(new SessionEndHookCommand());
    }
}
