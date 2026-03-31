namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IdentityCloudUrlArgument : Argument<string>
{
    public IdentityCloudUrlArgument() : base("url")
    {
        Description = "The URL of the Nitro backend (only needed for self-hosted or dedicated deployments)";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
