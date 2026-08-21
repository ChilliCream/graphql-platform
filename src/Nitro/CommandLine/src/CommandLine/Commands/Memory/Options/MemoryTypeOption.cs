namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryTypeOption : Option<string>
{
    public MemoryTypeOption() : base("--type")
    {
        Description = "The memory type (fact, decision, preference, reference, or custom)";
        Required = false;
    }
}
