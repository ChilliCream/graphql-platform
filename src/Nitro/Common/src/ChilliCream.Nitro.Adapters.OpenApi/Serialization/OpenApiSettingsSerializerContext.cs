using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.Adapters.OpenApi.Serialization;

[JsonSerializable(typeof(OpenApiEndpointSettings))]
[JsonSerializable(typeof(OpenApiModelSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class OpenApiSettingsSerializerContext : JsonSerializerContext;
