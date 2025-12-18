using System.Text.Json.Serialization;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Settings;

[JsonSerializable(typeof(CompositionSettings))]
[JsonSerializable(typeof(SourceSchemaSettings))]
[JsonSourceGenerationOptions(
    Converters = [typeof(JsonStringEnumConverter<DirectiveMergeBehavior>)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class SettingsJsonSerializerContext : JsonSerializerContext;
