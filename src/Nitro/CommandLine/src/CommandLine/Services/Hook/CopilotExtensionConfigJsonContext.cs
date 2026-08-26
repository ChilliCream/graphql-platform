using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CopilotExtensionConfig))]
internal sealed partial class CopilotExtensionConfigJsonContext : JsonSerializerContext;
