using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Output;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OutputEnvelope<CoordinateOperationsResult>))]
internal sealed partial class CoordinateOperationsJsonContext : JsonSerializerContext;
