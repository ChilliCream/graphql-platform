namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class IdArgument : Argument<string>
{
    public const string ArgumentName = "id";

    public IdArgument() : base(ArgumentName)
    {
        Description = "The resource ID";
        Arity = ArgumentArity.ExactlyOne;
    }
}
