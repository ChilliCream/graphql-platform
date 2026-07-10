using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NodeResolutionOption : Option<NodeResolution?>
{
    public NodeResolutionOption()
        : base("--node-resolution")
    {
        Description = "Choose whether Query.node identifiers are resolved by the gateway or a source schema";
        AcceptOnlyFromAmong("gateway", "source-schema");
        CustomParser = result => result.Tokens.Single().Value switch
        {
            "gateway" => NodeResolution.Gateway,
            "source-schema" => NodeResolution.SourceSchema,
            _ => null
        };
    }
}
