namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for preprocessing source schemas.
/// </summary>
public sealed class SourceSchemaPreprocessorOptions
{
    /// <summary>
    /// Applies inferred key directives to types that are returned by lookup fields.
    /// </summary>
    public bool ApplyInferredKeyDirectives { get; init; } = true;

    /// <summary>
    /// Applies key directives to types based on the keys defined on the interfaces that they
    /// implement.
    /// </summary>
    public bool InheritInterfaceKeys { get; init; } = true;

    /// <summary>
    /// TODO
    /// </summary>
    public bool FusionV1CompatibilityMode { get; init; }
}
