namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskDaysOption : Option<int?>
{
    public TaskDaysOption() : base("--days")
    {
        Description = "The staleness threshold in days (default 30)";
        Required = false;
    }
}
