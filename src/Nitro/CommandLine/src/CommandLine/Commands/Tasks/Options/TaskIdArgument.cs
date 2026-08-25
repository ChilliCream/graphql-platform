namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskIdArgument : Argument<string>
{
    public TaskIdArgument() : base("id")
    {
        Description = "The task ID";
    }
}
