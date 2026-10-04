namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Dependency;

internal sealed class TaskDependencyCommand : Command
{
    public TaskDependencyCommand() : base("dep")
    {
        Description = "Manage task dependencies.";

        Subcommands.Add(new AddTaskDependencyCommand());
        Subcommands.Add(new RemoveTaskDependencyCommand());
        Subcommands.Add(new ListTaskDependencyCommand());
        Subcommands.Add(new TreeTaskDependencyCommand());
        Subcommands.Add(new CyclesTaskDependencyCommand());
    }
}
