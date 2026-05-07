using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

[JsonSerializable(typeof(McpPromptSettings))]
[JsonSerializable(typeof(McpToolSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowOutOfOrderMetadataProperties = true)]
internal partial class McpSettingsSerializerContext : JsonSerializerContext;
