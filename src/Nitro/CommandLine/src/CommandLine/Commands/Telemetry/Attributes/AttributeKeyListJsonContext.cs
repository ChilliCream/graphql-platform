using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(ListAttributeKeysCommand.AttributeKeyListItem))]
internal partial class AttributeKeyListJsonContext : JsonSerializerContext;
