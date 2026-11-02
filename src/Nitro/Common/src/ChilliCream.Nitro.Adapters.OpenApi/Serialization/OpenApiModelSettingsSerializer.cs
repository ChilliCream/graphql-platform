using System.Text.Json;

namespace ChilliCream.Nitro.Adapters.OpenApi.Serialization;

public static class OpenApiModelSettingsSerializer
{
    public static JsonDocument Format(OpenApiModelSettings settings)
    {
        return JsonSerializer.SerializeToDocument(settings, OpenApiSettingsSerializerContext.Default.OpenApiModelSettings);
    }

    public static OpenApiModelSettings Parse(JsonDocument document)
    {
        return document.Deserialize(OpenApiSettingsSerializerContext.Default.OpenApiModelSettings)
            ?? throw new JsonException("Failed to deserialize model settings.");
    }
}
