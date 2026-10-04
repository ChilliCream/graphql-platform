namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryTagOption : Option<string[]>
{
    public MemoryTagOption() : base("--tag")
    {
        Description = "A tag; can be used multiple times";
        Required = false;
    }
}
