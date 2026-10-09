namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryRemoveTagOption : Option<string[]>
{
    public MemoryRemoveTagOption() : base("--remove-tag")
    {
        Description = "A tag to remove; can be used multiple times";
        Required = false;
    }
}
