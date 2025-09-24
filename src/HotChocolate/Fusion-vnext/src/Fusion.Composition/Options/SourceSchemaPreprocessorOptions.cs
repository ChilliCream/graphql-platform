namespace HotChocolate.Fusion.Options;

public sealed class SourceSchemaPreprocessorOptions
{
    /// <summary>
    /// Applies inferred key directives to types that are returned by lookup fields.
    /// </summary>
    public bool ApplyInferredKeyDirectives { get; set; } = true;

    /// <summary>
    /// Applies key directives to types based on the keys defined on the interfaces that they
    /// implement.
    /// </summary>
    public bool InheritInterfaceKeys { get; set; } = true;
}
