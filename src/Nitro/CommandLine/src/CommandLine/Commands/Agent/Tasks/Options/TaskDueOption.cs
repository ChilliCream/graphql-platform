namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskDueOption : Option<string>
{
    public TaskDueOption() : base("--due")
    {
        Description = "The due date as an ISO 8601 date or timestamp";
        Required = false;
    }
}
