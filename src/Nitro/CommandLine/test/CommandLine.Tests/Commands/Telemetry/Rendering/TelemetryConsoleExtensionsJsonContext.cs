using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Rendering;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(TelemetryConsoleExtensionsTests.Sample))]
internal partial class TelemetryConsoleExtensionsJsonContext : JsonSerializerContext;
