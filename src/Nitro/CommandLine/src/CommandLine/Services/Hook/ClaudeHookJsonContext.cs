using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Serializes Claude hook responses and deserializes hook payloads using their wire
/// field names. Null response properties are omitted.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ClaudeHookPayload))]
[JsonSerializable(typeof(ClaudeHookResponse))]
internal sealed partial class ClaudeHookJsonContext : JsonSerializerContext;
