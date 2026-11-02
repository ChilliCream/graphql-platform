using System.Text.Json;

namespace ChilliCream.Nitro.Adapters.OpenApi.Serialization;

public static class OpenApiEndpointSettingsSerializer
{
    public static JsonDocument Format(OpenApiEndpointSettings settings)
    {
        return JsonSerializer.SerializeToDocument(settings, OpenApiSettingsSerializerContext.Default.OpenApiEndpointSettings);
    }

    public static OpenApiEndpointSettings Parse(JsonDocument document)
    {
        return document.Deserialize(OpenApiSettingsSerializerContext.Default.OpenApiEndpointSettings)
            ?? throw new JsonException("Failed to deserialize endpoint settings.");
    }
}
