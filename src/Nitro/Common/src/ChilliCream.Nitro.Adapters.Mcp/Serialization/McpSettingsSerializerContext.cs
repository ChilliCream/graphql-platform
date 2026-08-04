using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Adapters.Mcp.Storage;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

[JsonSerializable(typeof(McpPromptSettings))]
[JsonSerializable(typeof(McpToolSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowOutOfOrderMetadataProperties = true,
    Converters = [typeof(McpAppViewVisibilityConverter)])]
internal partial class McpSettingsSerializerContext : JsonSerializerContext;

internal sealed class McpAppViewVisibilityConverter()
    : JsonStringEnumConverter<McpAppViewVisibility>(JsonNamingPolicy.CamelCase);
