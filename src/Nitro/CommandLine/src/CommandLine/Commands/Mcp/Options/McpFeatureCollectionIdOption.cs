namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;

internal sealed class McpFeatureCollectionIdOption : Option<string>
{
    public McpFeatureCollectionIdOption() : base("--mcp-feature-collection-id")
    {
        Description = "The ID of the MCP Feature Collection";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.McpFeatureCollectionId);
        this.NonEmptyStringsOnly();
    }
}
