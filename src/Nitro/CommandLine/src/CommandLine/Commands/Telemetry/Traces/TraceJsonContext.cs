using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(TraceJson))]
internal partial class TraceJsonContext : JsonSerializerContext;
