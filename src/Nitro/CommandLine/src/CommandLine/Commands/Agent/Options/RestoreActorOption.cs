namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class RestoreActorOption : Option<string>
{
    public RestoreActorOption() : base("--actor")
    {
        Description = "The acting agent, which does not block the restore; allocate one with `nitro agent login`";
        Required = false;
    }
}
