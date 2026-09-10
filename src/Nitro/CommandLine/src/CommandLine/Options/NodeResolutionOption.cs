using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NodeResolutionOption : Option<NodeResolution?>
{
    public const string OptionName = "--node-resolution";

    public NodeResolutionOption()
        : base(OptionName)
    {
        Description = "Choose whether Query.node identifiers are resolved by the router or a source schema (gateway is a legacy alias for router)";
        // TODO [17]: Remove the legacy gateway input alias.
        AcceptOnlyFromAmong("router", "gateway", "source-schema");
        CustomParser = result => result.Tokens.Single().Value switch
        {
            "router" or "gateway" => NodeResolution.Router,
            "source-schema" => NodeResolution.SourceSchema,
            _ => null
        };
    }
}
