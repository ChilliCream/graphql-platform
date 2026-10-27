namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryTextArgument : Argument<string?>
{
    public MemoryTextArgument() : base("text")
    {
        Description = "The memory text. Exactly one of the text argument or --file is required";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
