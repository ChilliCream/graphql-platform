namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryFileOption : Option<string>
{
    public MemoryFileOption() : base("--file")
    {
        Description = "A file to read the memory text from. Exactly one of the text argument or "
            + "--file is required";
        Required = false;
    }
}
