namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;

internal sealed class McpFeatureCollectionNameOption : Option<string>
{
    public McpFeatureCollectionNameOption() : base("--name")
    {
        Description = "The name of the MCP Feature Collection";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.McpFeatureCollectionName);
    }
}
