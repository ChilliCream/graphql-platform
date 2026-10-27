namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskAssigneeOption : Option<string>
{
    public TaskAssigneeOption() : base("--assignee")
    {
        Description = "The assignee";
        Required = false;
    }
}
