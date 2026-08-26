namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryActorOption : Option<string>
{
    public MemoryActorOption() : base("--actor")
    {
        Description = "The actor recorded on memory writes; inferred from the current session when omitted";
        Required = false;
    }
}
