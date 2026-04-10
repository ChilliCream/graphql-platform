namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class OptionalIdArgument : Argument<string?>
{
    public const string ArgumentName = "id";

    public OptionalIdArgument() : base(ArgumentName)
    {
        Description = "The resource ID";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
