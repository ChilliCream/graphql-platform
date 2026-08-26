namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskUntilOption : Option<string>
{
    public TaskUntilOption() : base("--until")
    {
        Description = "Hide the task from ready work until this ISO 8601 date or timestamp";
        Required = true;
    }
}
