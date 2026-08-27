using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Source-generated (de)serialization for the Codex hook and notify wire
/// types, kept separate from <see cref="ClaudeHookJsonContext"/> for the same
/// reason that one is kept separate from the CLI's own result DTO context:
/// <see cref="CodexHookPayload"/>/<see cref="CodexNotifyPayload"/>
/// deserialize Codex's own field-name casing (snake_case for hooks.json,
/// kebab-case for notify) and <see cref="CodexHookResponse"/> must omit null
/// properties so the neutral response is exactly <c>{}</c>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CodexHookPayload))]
[JsonSerializable(typeof(CodexHookResponse))]
[JsonSerializable(typeof(CodexNotifyPayload))]
internal sealed partial class CodexHookJsonContext : JsonSerializerContext;
