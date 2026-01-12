namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IdentityCloudUrlArgument : Argument<string>
{
    public IdentityCloudUrlArgument() : base("url")
    {
        Description = "The url of the api.";
        IsHidden = false;
        Arity = ArgumentArity.ZeroOrOne;
    }
}
