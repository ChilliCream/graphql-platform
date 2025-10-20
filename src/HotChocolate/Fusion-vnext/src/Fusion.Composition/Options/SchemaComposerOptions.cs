namespace HotChocolate.Fusion.Options;

public sealed class SchemaComposerOptions
{
    public required bool EnableGlobalObjectIdentification { get; init; }

    public SourceSchemaParserOptions Parser { get; init; } = new();

    public Dictionary<string, SourceSchemaPreprocessorOptions> PreprocessorOptions { get; init; } = [];
}
