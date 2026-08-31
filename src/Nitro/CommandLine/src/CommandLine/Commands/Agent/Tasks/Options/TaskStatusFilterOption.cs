namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskStatusFilterOption : Option<string[]>
{
    public TaskStatusFilterOption() : base("--status")
    {
        Description = "Filter by status; can be used multiple times";
        Required = false;
    }
}
