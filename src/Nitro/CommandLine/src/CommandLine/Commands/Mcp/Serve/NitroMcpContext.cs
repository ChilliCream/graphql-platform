namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve;

internal sealed class NitroMcpContext(string apiId, string stage)
{
    public string ApiId { get; } = apiId;

    public string Stage { get; } = stage;
}
