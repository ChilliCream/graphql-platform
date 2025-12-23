namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class OptionalIdArgument : Argument<string?>
{
    public OptionalIdArgument() : base("id")
    {
        Description = "The id";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
