using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiEndpointSettingsSerializer
{
    public static JsonDocument Format(OpenApiEndpointSettingsDto settings)
    {
        return JsonSerializer.SerializeToDocument(settings, OpenApiEndpointSettingsSerializerContext.Default.OpenApiEndpointSettingsDto);
    }

    public static OpenApiEndpointSettingsDto Parse(JsonDocument document)
    {
        return document.Deserialize(OpenApiEndpointSettingsSerializerContext.Default.OpenApiEndpointSettingsDto)
            ?? throw new JsonException("Failed to deserialize endpoint settings.");
    }
}

[JsonSerializable(typeof(OpenApiEndpointSettingsDto))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class OpenApiEndpointSettingsSerializerContext : JsonSerializerContext;
