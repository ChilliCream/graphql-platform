using System.Text.Json.Serialization;

namespace HotChocolate.Adapters.OpenApi;

[JsonSerializable(typeof(OpenApiEndpointSettingsDto))]
[JsonSerializable(typeof(OpenApiModelSettingsDto))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class OpenApiSettingsSerializerContext : JsonSerializerContext;
