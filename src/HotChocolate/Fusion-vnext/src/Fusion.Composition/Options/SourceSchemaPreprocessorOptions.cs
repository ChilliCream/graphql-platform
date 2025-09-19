namespace HotChocolate.Fusion.Options;

public sealed class SourceSchemaPreprocessorOptions
{
    /// <summary>
    /// Applies inferred key directives to types that are returned by lookup fields.
    /// </summary>
    public bool ApplyInferredKeyDirectives { get; set; } = true;
}
