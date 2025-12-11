namespace ChilliCream.Nitro.CommandLine.Fusion.Settings;

internal sealed record SourceSchemaSettings
{
    public required string Name { get; init; }

    public Version? Version { get; init; }

    public ParserSettings? Parser { get; init; }

    public PreprocessorSettings? Preprocessor { get; init; }

    public SatisfiabilitySettings? Satisfiability { get; init; }
}
