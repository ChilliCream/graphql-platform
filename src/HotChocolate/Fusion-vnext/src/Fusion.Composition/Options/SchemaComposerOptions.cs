namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for composing source schemas.
/// </summary>
public sealed class SchemaComposerOptions
{
    /// <summary>
    /// Configuration options for parsing source schemas.
    /// </summary>
    public SourceSchemaParserOptions Parser { get; } = new();

    /// <summary>
    /// Configuration options for preprocessing source schemas.
    /// </summary>
    public Dictionary<string, SourceSchemaPreprocessorOptions> Preprocessor { get; } = new();

    /// <summary>
    /// Configuration options for merging source schemas.
    /// </summary>
    public SourceSchemaMergerOptions Merger { get; } = new();
}
