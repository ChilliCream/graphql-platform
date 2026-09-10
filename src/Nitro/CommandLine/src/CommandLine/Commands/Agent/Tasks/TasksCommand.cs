using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Comment;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Config;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Dependency;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Epic;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Label;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class TasksCommand : Command
{
    public TasksCommand() : base("tasks")
    {
        Description = "Create and manage tasks for coding agents.";

        Subcommands.Add(new BlockedTaskCommand());
        Subcommands.Add(new CloseTaskCommand());
        Subcommands.Add(new TaskCommentCommand());
        Subcommands.Add(new TaskConfigCommand());
        Subcommands.Add(new CountTaskCommand());
        Subcommands.Add(new CreateTaskCommand());
        Subcommands.Add(new DeferTaskCommand());
        Subcommands.Add(new DeleteTaskCommand());
        Subcommands.Add(new TaskDependencyCommand());
        Subcommands.Add(new DoctorTaskCommand());
        Subcommands.Add(new TaskEpicCommand());
        Subcommands.Add(new TaskLabelCommand());
        Subcommands.Add(new LintTaskCommand());
        Subcommands.Add(new ListTaskCommand());
        Subcommands.Add(new QuickTaskCommand());
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
