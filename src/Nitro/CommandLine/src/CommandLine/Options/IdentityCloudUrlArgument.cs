namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IdentityCloudUrlArgument : Argument<string>
{
    public IdentityCloudUrlArgument() : base("url")
    {
        Description = "The URL of the API.";
        IsHidden = false;
        Arity = ArgumentArity.ZeroOrOne;
    }
}
