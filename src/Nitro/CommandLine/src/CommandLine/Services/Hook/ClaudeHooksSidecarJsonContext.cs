using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ClaudeHooksSidecarFile))]
internal sealed partial class ClaudeHooksSidecarJsonContext : JsonSerializerContext;
