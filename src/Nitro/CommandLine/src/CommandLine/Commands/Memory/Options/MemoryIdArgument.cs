namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryIdArgument : Argument<string>
{
    public MemoryIdArgument() : base("id")
    {
        Description = "The memory ID";
    }
}
