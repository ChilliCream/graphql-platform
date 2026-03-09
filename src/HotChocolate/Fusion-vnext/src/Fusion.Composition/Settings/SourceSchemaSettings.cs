namespace HotChocolate.Fusion;

internal sealed record SourceSchemaSettings
{
    public required string Name { get; init; }

    public Version? Version { get; init; }

    public ParserSettings? Parser { get; init; }

    public PreprocessorSettings? Preprocessor { get; init; }

    public SatisfiabilitySettings? Satisfiability { get; init; }

    internal sealed record ParserSettings
    {
        public bool? EnableSchemaValidation { get; init; }
    }

    internal sealed record PreprocessorSettings
    {
        public bool? EnableSchemaValidation { get; init; }

        public bool? InferKeysFromLookups { get; init; }

        public bool? InheritInterfaceKeys { get; init; }
    }

    internal sealed record SatisfiabilitySettings
    {
        public Dictionary<string, List<string>>? IgnoredNonAccessibleFields { get; init; }
    }
}
