namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class RequiredActorOption : Option<string>
{
    public RequiredActorOption() : base("--actor")
    {
        Description = "The actor to register; allocate one with `nitro agent login`";
        Required = true;
    }
}
