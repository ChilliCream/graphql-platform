namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryActorOption : Option<string>
{
    public MemoryActorOption() : base("--actor")
    {
        Description = "The actor performing this command; allocate one with `nitro agent login`";
        Required = true;
    }
}
