namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskStatusOption : Option<string>
{
    public TaskStatusOption() : base("--status")
    {
        Description = "The task status (open, in_progress, blocked, deferred, closed, or custom)";
        Required = false;
    }
}
