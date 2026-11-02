namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskByOption : Option<string>
{
    public TaskByOption() : base("--by")
    {
        Description = "Group counts by: status, type, priority, assignee, or label";
        Required = false;
    }
}
