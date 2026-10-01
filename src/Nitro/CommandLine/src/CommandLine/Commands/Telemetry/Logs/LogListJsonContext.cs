using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(ListLogsCommand.LogListItem))]
internal partial class LogListJsonContext : JsonSerializerContext;
