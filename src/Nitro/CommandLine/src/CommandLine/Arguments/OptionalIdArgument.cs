namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class OptionalIdArgument : Argument<string?>
{
    public OptionalIdArgument() : base("id")
    {
        Description = "The ID";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
