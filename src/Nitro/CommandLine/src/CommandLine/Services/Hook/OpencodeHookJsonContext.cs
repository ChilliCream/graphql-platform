using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Source-generated serialization for the opencode shim request and response.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OpencodeHookPayload))]
[JsonSerializable(typeof(OpencodeHookResponse))]
internal sealed partial class OpencodeHookJsonContext : JsonSerializerContext;
