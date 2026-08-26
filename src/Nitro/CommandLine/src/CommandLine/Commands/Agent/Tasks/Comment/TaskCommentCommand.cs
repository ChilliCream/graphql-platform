namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Comment;

internal sealed class TaskCommentCommand : Command
{
    public TaskCommentCommand() : base("comment")
    {
        Description = "Add and list task comments.";

        Subcommands.Add(new AddTaskCommentCommand());
        Subcommands.Add(new ListTaskCommentCommand());
    }
}
