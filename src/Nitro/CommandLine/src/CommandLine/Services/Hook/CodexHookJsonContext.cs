using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Serializes Codex hook responses and deserializes hook and notify payloads using their wire
/// field names. Null response properties are omitted.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CodexHookPayload))]
[JsonSerializable(typeof(CodexHookResponse))]
[JsonSerializable(typeof(CodexNotifyPayload))]
internal sealed partial class CodexHookJsonContext : JsonSerializerContext;
