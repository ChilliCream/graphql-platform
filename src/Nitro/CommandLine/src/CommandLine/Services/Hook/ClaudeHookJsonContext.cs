using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Source-generated (de)serialization for the Claude hook wire types, kept
/// separate from <c>JsonSourceGenerationContext</c> in
/// <c>ChilliCream.Nitro.CommandLine.Results</c>: that context serializes
/// this CLI's own <c>--output json</c> result DTOs in camelCase with
/// nothing omitted, while <see cref="ClaudeHookPayload"/> deserializes
/// Claude's own snake_case field names and <see cref="ClaudeHookResponse"/>
/// must omit null properties so the neutral response is exactly
/// <c>{}</c>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ClaudeHookPayload))]
[JsonSerializable(typeof(ClaudeHookResponse))]
internal sealed partial class ClaudeHookJsonContext : JsonSerializerContext;
