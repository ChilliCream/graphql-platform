namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskAddLabelOption : Option<string[]>
{
    public TaskAddLabelOption() : base("--add-label")
    {
        Description = "A label to add; can be used multiple times";
        Required = false;
    }
}
