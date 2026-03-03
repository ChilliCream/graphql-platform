using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

[JsonSerializable(typeof(ProjectSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ProjectSettingsJsonContext : JsonSerializerContext;
