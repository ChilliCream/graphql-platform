using System.Text.Json;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

public static class McpPromptSettingsSerializer
{
    public static JsonDocument Format(McpPromptSettings settings)
    {
        return JsonSerializer.SerializeToDocument(
            settings,
            McpSettingsSerializerContext.Default.McpPromptSettings);
    }

    public static McpPromptSettings Parse(JsonDocument document)
    {
        return document.Deserialize(McpSettingsSerializerContext.Default.McpPromptSettings)
            ?? throw new JsonException("Failed to deserialize prompt settings.");
    }
}
