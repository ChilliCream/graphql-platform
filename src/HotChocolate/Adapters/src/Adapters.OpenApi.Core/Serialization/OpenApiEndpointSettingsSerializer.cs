using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiEndpointSettingsSerializer
{
    public static JsonDocument Format(OpenApiEndpointSettingsDto settings)
    {
        return JsonSerializer.SerializeToDocument(settings, OpenApiSettingsSerializerContext.Default.OpenApiEndpointSettingsDto);
    }

    public static OpenApiEndpointSettingsDto Parse(JsonDocument document)
    {
        return document.Deserialize(OpenApiSettingsSerializerContext.Default.OpenApiEndpointSettingsDto)
            ?? throw new JsonException("Failed to deserialize endpoint settings.");
    }
}
