namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryTextOption : Option<string>
{
    public MemoryTextOption() : base("--text")
    {
        Description = "The new memory text. At most one of --text or --file may be given";
        Required = false;
    }
}
