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
        SatisfiabilitySettings? satisfiability = null)
    {
        Preprocessor = preprocessor ?? new();
        Merger = merger ?? new();
        Satisfiability = satisfiability ?? new();
    }

    public PreprocessorSettings Preprocessor { get; init; }

    public MergerSettings Merger { get; init; }

    public SatisfiabilitySettings Satisfiability { get; init; }

    internal sealed record PreprocessorSettings
    {
        public HashSet<string>? ExcludeByTag { get; set; } = [];
    }

    internal sealed record MergerSettings
    {
        public bool? AddFusionDefinitions { get; init; }

        public DirectiveMergeBehavior? CacheControlMergeBehavior { get; init; }

        public bool? EnableGlobalObjectIdentification { get; set; }

        public bool? RemoveUnreferencedDefinitions { get; init; }

        public DirectiveMergeBehavior? TagMergeBehavior { get; init; }
    }

    internal sealed record SatisfiabilitySettings
    {
        public bool? IncludeSatisfiabilityPaths { get; set; }
    }
}
