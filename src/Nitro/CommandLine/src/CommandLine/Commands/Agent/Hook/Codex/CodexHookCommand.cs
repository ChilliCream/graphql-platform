namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

internal sealed class CodexHookCommand : Command
{
    public CodexHookCommand() : base("codex")
    {
        Description = "Adapt Codex CLI turn-boundary hook and notify events.";

        Subcommands.Add(new SessionStartHookCommand());
        Subcommands.Add(new UserPromptSubmitHookCommand());
        Subcommands.Add(new SessionEndHookCommand());
        Subcommands.Add(new NotifyHookCommand());
    }
}
