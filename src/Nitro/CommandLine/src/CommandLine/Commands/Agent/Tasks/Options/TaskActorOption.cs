namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskActorOption : Option<string>
{
    public TaskActorOption() : base("--actor")
    {
        Description = "The actor recorded on the audit log; allocate one with `nitro agent login`";
        Required = true;
    }
}
