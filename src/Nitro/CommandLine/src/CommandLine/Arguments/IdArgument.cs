namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class IdArgument : Argument<string>
{
    public IdArgument() : base("id")
    {
        Description = "The id";
        Arity = ArgumentArity.ExactlyOne;
    }
}
