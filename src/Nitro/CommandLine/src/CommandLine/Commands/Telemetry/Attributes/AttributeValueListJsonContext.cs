using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(ListAttributeValuesCommand.AttributeValueListItem))]
internal partial class AttributeValueListJsonContext : JsonSerializerContext;
