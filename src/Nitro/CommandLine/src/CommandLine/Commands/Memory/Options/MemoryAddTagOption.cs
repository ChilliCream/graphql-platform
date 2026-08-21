namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryAddTagOption : Option<string[]>
{
    public MemoryAddTagOption() : base("--add-tag")
    {
        Description = "A tag to add; can be used multiple times";
        Required = false;
    }
}
