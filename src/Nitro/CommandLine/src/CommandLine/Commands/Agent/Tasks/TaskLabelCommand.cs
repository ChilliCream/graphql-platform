namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class TaskLabelCommand : Command
{
    public TaskLabelCommand() : base("label")
    {
        Description = "Add, remove, and list task labels.";

        Subcommands.Add(new AddTaskLabelCommand());
        Subcommands.Add(new RemoveTaskLabelCommand());
        Subcommands.Add(new ListTaskLabelCommand());
    }
}
