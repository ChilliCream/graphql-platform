using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Fusion.Settings;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Fusion;

[JsonSerializable(typeof(CompositionSettings))]
[JsonSerializable(typeof(SourceSchemaSettings))]
[JsonSourceGenerationOptions(
    Converters = [typeof(JsonStringEnumConverter<DirectiveMergeBehavior>)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class JsonSourceGenerationContext : JsonSerializerContext;
