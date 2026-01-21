using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Settings;

internal sealed record CompositionSettings
{
    public PreprocessorSettings Preprocessor { get; init; } = new();

    public MergerSettings Merger { get; init; } = new();

    public SatisfiabilitySettings Satisfiability { get; init; } = new();

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
