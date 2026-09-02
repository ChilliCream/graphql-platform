namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class TakeoverFromActorOption : Option<string>
{
    public TakeoverFromActorOption() : base("--from")
    {
        Description = "The actor whose mail and tasks to take over";
        Required = true;
    }
}
