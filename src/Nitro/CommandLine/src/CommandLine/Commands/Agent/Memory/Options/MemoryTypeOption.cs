namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryTypeOption : Option<string>
{
    public MemoryTypeOption() : base("--type")
    {
        Description = "The memory type (fact, decision, preference, reference, or custom)";
        Required = false;
    }
}
