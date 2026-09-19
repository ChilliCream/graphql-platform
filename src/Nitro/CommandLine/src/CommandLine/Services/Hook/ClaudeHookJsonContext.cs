using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Source-generated (de)serialization for the Claude hook wire types.
/// <see cref="ClaudeHookPayload"/> deserializes Claude's own snake_case field
/// names, and <see cref="ClaudeHookResponse"/> must omit null properties so the
/// neutral response is exactly <c>{}</c>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ClaudeHookPayload))]
[JsonSerializable(typeof(ClaudeHookResponse))]
internal sealed partial class ClaudeHookJsonContext : JsonSerializerContext;
