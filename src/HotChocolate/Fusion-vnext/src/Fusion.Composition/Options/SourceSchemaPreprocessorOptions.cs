namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for preprocessing source schemas.
/// </summary>
public sealed class SourceSchemaPreprocessorOptions
{
    /// <summary>
    /// Applies inferred key directives to types that are returned by lookup fields.
    /// </summary>
    public bool ApplyInferredKeyDirectives { get; set; } = true;
}
