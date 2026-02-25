using System.Text.Json;

namespace HotChocolate.Adapters.Mcp.Serialization;

public static class McpPromptSettingsSerializer
{
    public static JsonDocument Format(McpPromptSettingsDto settings)
    {
        return JsonSerializer.SerializeToDocument(
            settings,
            McpSettingsSerializerContext.Default.McpPromptSettingsDto);
    }

    public static McpPromptSettingsDto Parse(JsonDocument document)
    {
        return document.Deserialize(McpSettingsSerializerContext.Default.McpPromptSettingsDto)
            ?? throw new JsonException("Failed to deserialize prompt settings.");
    }
}
