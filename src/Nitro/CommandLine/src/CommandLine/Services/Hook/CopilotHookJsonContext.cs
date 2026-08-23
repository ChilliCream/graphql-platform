using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Source-generated (de)serialization for the Copilot hook wire types, kept
/// separate from <see cref="ClaudeHookJsonContext"/>/<see cref="CodexHookJsonContext"/>
/// for the same per-harness reason: <see cref="CopilotHookPayload"/> reads
/// Copilot's own camelCase field names, and <see cref="CopilotHookResponse"/>
/// must omit its null property so the neutral response is exactly
/// <c>{}</c>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CopilotHookPayload))]
[JsonSerializable(typeof(CopilotHookResponse))]
internal sealed partial class CopilotHookJsonContext : JsonSerializerContext;
