using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CodexHooksSidecarFile))]
internal sealed partial class CodexHooksSidecarJsonContext : JsonSerializerContext;
