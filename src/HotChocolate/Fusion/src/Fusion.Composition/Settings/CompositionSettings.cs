using System.Text.Json.Serialization;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

internal sealed record CompositionSettings
{
    // If the composition settings are older / have a different format,
    // this ensures that all of the settings properties are initialized
    // when attempting to deserialize from JSON.
    [JsonConstructor]
    public CompositionSettings(
        PreprocessorSettings? preprocessor = null,
        MergerSettings? merger = null,
        SatisfiabilitySettings? satisfiability = null,
        ApolloFederationCompatibilitySettings? apolloFederationCompatibility = null)
    {
        Preprocessor = preprocessor ?? new();
        Merger = merger ?? new();
        Satisfiability = satisfiability ?? new();
        ApolloFederationCompatibility = apolloFederationCompatibility ?? new();
    }

    public PreprocessorSettings Preprocessor { get; init; }

    public MergerSettings Merger { get; init; }

    public SatisfiabilitySettings Satisfiability { get; init; }

    public ApolloFederationCompatibilitySettings ApolloFederationCompatibility { get; init; }

    internal sealed record PreprocessorSettings
    {
        public HashSet<string>? ExcludeByTag { get; set; }
    }

    internal sealed record MergerSettings
    {
        public bool? AddFusionDefinitions { get; init; }

        public DirectiveMergeBehavior? CacheControlMergeBehavior { get; set; }

        public bool? EnableGlobalObjectIdentification { get; set; }

        public bool? AddNodesField { get; set; }

        public NodeResolution? NodeResolution { get; set; }

        public bool? RemoveUnreferencedDefinitions { get; init; }

        public DirectiveMergeBehavior? TagMergeBehavior { get; set; }
    }

    internal sealed record SatisfiabilitySettings
    {
        public bool? IncludeSatisfiabilityPaths { get; set; }
    }

    internal sealed record ApolloFederationCompatibilitySettings
    {
        public bool? AllowNonResolvableInterfaceObjects { get; set; }

        public ShareableFieldRuntimeTypeRouting? ShareableFieldRuntimeTypeRouting { get; set; }
    }
}
