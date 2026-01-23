using ChilliCream.Nitro.CommandLine.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;

internal sealed class McpFeatureCollectionDetailPrompt
{
    private readonly IMcpFeatureCollectionDetailPrompt_McpFeatureCollection _data;

    private McpFeatureCollectionDetailPrompt(IMcpFeatureCollectionDetailPrompt_McpFeatureCollection data)
    {
        _data = data;
    }

    public McpFeatureCollectionDetailPromptResult ToObject(string[] formats)
    {
        return new McpFeatureCollectionDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name
        };
    }

    public static McpFeatureCollectionDetailPrompt From(IMcpFeatureCollectionDetailPrompt_McpFeatureCollection data) => new(data);

    public class McpFeatureCollectionDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }
    }
}
