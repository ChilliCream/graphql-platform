using System.Text.Json.Serialization;

namespace HotChocolate.Adapters.Mcp.Serialization;

[JsonSerializable(typeof(McpPromptSettingsDto))]
[JsonSerializable(typeof(McpToolSettingsDto))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class McpSettingsSerializerContext : JsonSerializerContext;
