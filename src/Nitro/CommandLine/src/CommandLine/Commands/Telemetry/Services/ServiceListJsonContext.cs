using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Services;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(ListServicesCommand.ServiceListItem))]
internal partial class ServiceListJsonContext : JsonSerializerContext;
