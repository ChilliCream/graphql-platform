namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskDeferUntilOption : Option<string>
{
    public TaskDeferUntilOption() : base("--defer-until")
    {
        Description = "Hide the task from ready work until this ISO 8601 date or timestamp";
        Required = false;
    }
}
