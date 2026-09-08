namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class TakeoverActorOption : Option<string>
{
    public TakeoverActorOption() : base("--actor")
    {
        Description = "The actor taking over the mail and tasks";
        Required = true;
    }
}
