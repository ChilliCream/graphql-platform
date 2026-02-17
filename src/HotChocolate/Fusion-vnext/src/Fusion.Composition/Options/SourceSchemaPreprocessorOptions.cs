namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for preprocessing source schemas.
/// </summary>
public sealed class SourceSchemaPreprocessorOptions
{
    /// <summary>
    /// Enables schema validation when preprocessing source schemas.
    /// </summary>
    public bool EnableSchemaValidation { get; set; } = true;

    /// <summary>
    /// A list of tags used to exclude type system members from composition.
    /// Any members annotated with these tags will be excluded.
    /// </summary>
    public HashSet<string> ExcludeByTag { get; set; } = [];

    /// <summary>
    /// Applies inferred key directives to types that are returned by lookup fields.
    /// </summary>
    public bool InferKeysFromLookups { get; set; } = true;

    /// <summary>
    /// Applies key directives to types based on the keys defined on the interfaces that they
    /// implement.
    /// </summary>
    public bool InheritInterfaceKeys { get; set; } = true;
}
