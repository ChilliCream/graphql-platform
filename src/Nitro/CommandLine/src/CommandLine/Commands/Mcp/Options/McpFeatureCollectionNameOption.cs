using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;

internal sealed class McpFeatureCollectionNameOption : Option<string>
{
    public McpFeatureCollectionNameOption() : base("--name")
    {
        Description = "The name of the MCP Feature Collection";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("MCP_FEATURE_COLLECTION_NAME");
    }
}
