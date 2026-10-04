using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

[JsonSerializable(typeof(OpencodeHooksSidecarFile))]
internal sealed partial class OpencodeHooksSidecarJsonContext : JsonSerializerContext;
