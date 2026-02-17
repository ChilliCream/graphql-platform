using System.Text.Json;

namespace HotChocolate.Adapters.Mcp.Serialization;

public static class McpToolSettingsSerializer
{
    public static JsonDocument Format(McpToolSettingsDto settings)
    {
        return JsonSerializer.SerializeToDocument(
            settings,
            McpSettingsSerializerContext.Default.McpToolSettingsDto);
    }

    public static McpToolSettingsDto Parse(JsonDocument document)
    {
        return document.Deserialize(McpSettingsSerializerContext.Default.McpToolSettingsDto)
            ?? throw new JsonException("Failed to deserialize tool settings.");
    }
}
