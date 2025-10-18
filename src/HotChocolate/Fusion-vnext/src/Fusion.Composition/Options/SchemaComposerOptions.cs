namespace HotChocolate.Fusion.Options;

public sealed class SchemaComposerOptions
{
    public required bool EnableGlobalObjectIdentification { get; init; }

    public SourceSchemaParserOptions Parser { get; init; } = new();

    public SourceSchemaPreprocessorOptions Preprocessor { get; init; } = new();
}
