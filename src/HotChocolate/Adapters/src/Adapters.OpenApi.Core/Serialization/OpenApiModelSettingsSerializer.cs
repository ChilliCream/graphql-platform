using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiModelSettingsSerializer
{
    public static JsonDocument Format(OpenApiModelSettingsDto settings)
    {
        return JsonSerializer.SerializeToDocument(settings, OpenApiSettingsSerializerContext.Default.OpenApiModelSettingsDto);
    }

    public static OpenApiModelSettingsDto Parse(JsonDocument document)
    {
        return document.Deserialize(OpenApiSettingsSerializerContext.Default.OpenApiModelSettingsDto)
            ?? throw new JsonException("Failed to deserialize model settings.");
    }
}
