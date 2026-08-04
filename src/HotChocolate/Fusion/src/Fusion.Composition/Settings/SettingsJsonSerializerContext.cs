using System.Text.Json.Serialization;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

[JsonSerializable(typeof(CompositionSettings))]
[JsonSerializable(typeof(CompositionSettings.PreprocessorSettings), TypeInfoPropertyName = "CompositionPreprocessorSettings")]
[JsonSerializable(typeof(CompositionSettings.SatisfiabilitySettings), TypeInfoPropertyName = "CompositionSatisfiabilitySettings")]
[JsonSerializable(
    typeof(CompositionSettings.ApolloFederationCompatibilitySettings),
    TypeInfoPropertyName = "CompositionApolloFederationCompatibilitySettings")]
[JsonSerializable(typeof(SourceSchemaSettings))]
[JsonSerializable(typeof(SourceSchemaSettings.PreprocessorSettings), TypeInfoPropertyName = "SourceSchemaPreprocessorSettings")]
[JsonSerializable(typeof(SourceSchemaSettings.SatisfiabilitySettings), TypeInfoPropertyName = "SourceSchemaSatisfiabilitySettings")]
[JsonSourceGenerationOptions(
    Converters =
    [
        typeof(JsonStringEnumConverter<DirectiveMergeBehavior>),
        typeof(JsonStringEnumConverter<NodeResolution>),
        typeof(JsonStringEnumConverter<ShareableFieldRuntimeTypeRouting>)
    ],
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class SettingsJsonSerializerContext : JsonSerializerContext;
