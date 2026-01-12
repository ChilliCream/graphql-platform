namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class McpCommand : Command
{
    public McpCommand() : base("mcp")
    {
        Description = "Manage MCP Feature Collections";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateMcpFeatureCollectionCommand());
        AddCommand(new DeleteMcpFeatureCollectionCommand());
        AddCommand(new ListMcpFeatureCollectionCommand());
        AddCommand(new UploadMcpFeatureCollectionCommand());
        AddCommand(new PublishMcpFeatureCollectionCommand());
        AddCommand(new ValidateMcpFeatureCollectionCommand());
    }
}
