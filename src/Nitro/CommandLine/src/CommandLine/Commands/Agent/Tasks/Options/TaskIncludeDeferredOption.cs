namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskIncludeDeferredOption : Option<bool>
{
    public TaskIncludeDeferredOption() : base("--include-deferred")
    {
        Description = "Include deferred tasks";
        Required = false;
    }
}
