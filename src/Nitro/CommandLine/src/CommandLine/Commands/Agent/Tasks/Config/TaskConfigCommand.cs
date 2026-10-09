namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Config;

internal sealed class TaskConfigCommand : Command
{
    public TaskConfigCommand() : base("config")
    {
        Description = "View and manage task workspace configuration.";

        Subcommands.Add(new GetTaskConfigCommand());
        Subcommands.Add(new ListTaskConfigCommand());
        Subcommands.Add(new SetTaskConfigCommand());
    }
}
