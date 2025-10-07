using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Fusion.Commands;

namespace ChilliCream.Nitro.CommandLine.Fusion;

[JsonSerializable(typeof(ComposeCommand.CompositionSettings))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext;
