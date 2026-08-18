namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class TaskCommand : Command
{
    public TaskCommand() : base("task")
    {
        Description = "Create and manage tasks for coding agents.";

        Subcommands.Add(new BlockedTaskCommand());
        Subcommands.Add(new BoardTaskCommand());
        Subcommands.Add(new CloseTaskCommand());
        Subcommands.Add(new TaskCommentCommand());
        Subcommands.Add(new TaskConfigCommand());
        Subcommands.Add(new CountTaskCommand());
        Subcommands.Add(new CreateTaskCommand());
        Subcommands.Add(new DeferTaskCommand());
        Subcommands.Add(new DeleteTaskCommand());
        Subcommands.Add(new TaskDependencyCommand());
        Subcommands.Add(new TaskEpicCommand());
        Subcommands.Add(new InitTaskCommand());
        Subcommands.Add(new TaskLabelCommand());
        Subcommands.Add(new ListTaskCommand());
        Subcommands.Add(new ReadyTaskCommand());
        Subcommands.Add(new ReopenTaskCommand());
        Subcommands.Add(new SearchTaskCommand());
        Subcommands.Add(new ShowTaskCommand());
        Subcommands.Add(new StaleTaskCommand());
        Subcommands.Add(new StatsTaskCommand());
        Subcommands.Add(new UndeferTaskCommand());
        Subcommands.Add(new UpdateTaskCommand());
        Subcommands.Add(new WhereTaskCommand());
    }
}
