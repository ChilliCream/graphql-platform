namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class McpFeatureCollectionIdOption : Option<string>
{
    public McpFeatureCollectionIdOption() : base("--mcp-feature-collection-id")
    {
        Description = "The id of the MCP Feature Collection";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("MCP_FEATURE_COLLECTION_ID");
    }
}
