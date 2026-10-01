using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(ShowLogCommand.LogDetail))]
internal partial class LogDetailJsonContext : JsonSerializerContext;
