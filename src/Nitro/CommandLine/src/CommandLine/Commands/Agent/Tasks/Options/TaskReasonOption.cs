namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskReasonOption : Option<string>
{
    public TaskReasonOption() : base("--reason")
    {
        Description = "The reason recorded for this change";
        Required = false;
    }
}
