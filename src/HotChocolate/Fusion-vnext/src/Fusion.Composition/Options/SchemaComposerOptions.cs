namespace HotChocolate.Fusion.Options;

public sealed class SchemaComposerOptions
{
    public required bool EnableGlobalObjectIdentification { get; init; }

    public SourceSchemaPreprocessorOptions Preprocessor { get; init; } = new();
}
