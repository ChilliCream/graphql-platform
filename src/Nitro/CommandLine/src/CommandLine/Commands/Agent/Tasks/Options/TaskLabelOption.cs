namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskLabelOption : Option<string[]>
{
    public TaskLabelOption() : base("--label")
    {
        Description = "A label; can be used multiple times";
        Required = false;
    }
}
