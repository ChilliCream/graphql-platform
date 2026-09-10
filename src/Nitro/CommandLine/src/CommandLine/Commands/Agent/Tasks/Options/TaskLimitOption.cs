namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskLimitOption : Option<int?>
{
    public TaskLimitOption() : base("--limit")
    {
        Description = "The maximum number of tasks to show";
        Required = false;
    }
}
