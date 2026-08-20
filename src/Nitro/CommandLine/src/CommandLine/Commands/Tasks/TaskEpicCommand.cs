namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class TaskEpicCommand : Command
{
    public TaskEpicCommand() : base("epic")
    {
        Description = "View and close epics based on child task completion.";

        Subcommands.Add(new CloseEligibleTaskEpicCommand());
        Subcommands.Add(new StatusTaskEpicCommand());
    }
}
