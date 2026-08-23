namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class TaskCommentCommand : Command
{
    public TaskCommentCommand() : base("comment")
    {
        Description = "Add and list task comments.";

        Subcommands.Add(new AddTaskCommentCommand());
        Subcommands.Add(new ListTaskCommentCommand());
    }
}
