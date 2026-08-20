namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskTitleArgument : Argument<string>
{
    public TaskTitleArgument() : base("title")
    {
        Description = "The task title";
    }
}
