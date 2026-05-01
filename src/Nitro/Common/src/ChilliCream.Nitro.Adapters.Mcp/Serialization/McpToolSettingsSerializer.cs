using System.Text.Json;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

public static class McpToolSettingsSerializer
{
    public static JsonDocument Format(McpToolSettings settings)
    {
        return JsonSerializer.SerializeToDocument(
            settings,
            McpSettingsSerializerContext.Default.McpToolSettings);
    }

    public static McpToolSettings Parse(JsonDocument document)
    {
        return document.Deserialize(McpSettingsSerializerContext.Default.McpToolSettings)
            ?? throw new JsonException("Failed to deserialize tool settings.");
    }
}
