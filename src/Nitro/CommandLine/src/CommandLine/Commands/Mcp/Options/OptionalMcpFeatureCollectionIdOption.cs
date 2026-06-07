namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;

internal sealed class OptionalMcpFeatureCollectionIdOption : McpFeatureCollectionIdOption
{
    public OptionalMcpFeatureCollectionIdOption() : base()
    {
        Required = false;
    }
}
