namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

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
