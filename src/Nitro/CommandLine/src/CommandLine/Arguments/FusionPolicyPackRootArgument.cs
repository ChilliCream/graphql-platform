namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionPolicyPackRootArgument : Argument<string>
{
    public const string ArgumentName = "ROOT";

    public FusionPolicyPackRootArgument() : base(ArgumentName)
    {
        Description = "The root directory of the Rego policy authoring layout "
            + "(<root>/<package>/*.rego, <root>/<package>.graphql, <root>/lib/*.rego)";
        this.LegalFilePathsOnly();
    }
}
