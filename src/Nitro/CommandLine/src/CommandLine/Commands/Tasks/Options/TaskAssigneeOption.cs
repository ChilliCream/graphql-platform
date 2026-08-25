namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskAssigneeOption : Option<string>
{
    public TaskAssigneeOption() : base("--assignee")
    {
        Description = "The assignee";
        Required = false;
    }
}
