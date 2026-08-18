namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskTitleOption : Option<string>
{
    public TaskTitleOption() : base("--title")
    {
        Description = "The task title";
        Required = false;
    }
}
