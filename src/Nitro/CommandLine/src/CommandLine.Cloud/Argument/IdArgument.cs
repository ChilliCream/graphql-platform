namespace ChilliCream.Nitro.CLI.Option;

internal sealed class IdArgument : Argument<string>
{
    public IdArgument() : base("id")
    {
        Description = "The id";
        Arity = ArgumentArity.ExactlyOne;
    }
}

internal sealed class OptionalIdArgument : Argument<string?>
{
    public OptionalIdArgument() : base("id")
    {
        Description = "The id";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
