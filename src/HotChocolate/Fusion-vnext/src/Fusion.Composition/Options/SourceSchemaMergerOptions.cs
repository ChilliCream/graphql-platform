namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for merging source schemas.
/// </summary>
public sealed class SourceSchemaMergerOptions
{
    /// <summary>
    /// Adds Fusion-specific definitions to the merged schema.
    /// </summary>
    public bool AddFusionDefinitions { get; set; } = true;

    /// <summary>
    /// Enables the inclusion of Global Object Identification fields.
    /// </summary>
    public bool EnableGlobalObjectIdentification { get; set; }

    /// <summary>
    /// Removes types that are not referenced in the final merged schema.
    /// </summary>
    public bool RemoveUnreferencedTypes { get; set; } = true;

    /// <summary>
    /// Defines how to handle tag directives when merging source schemas.
    /// </summary>
    public TagMergeBehavior TagMergeBehavior { get; set; } = TagMergeBehavior.IncludePrivate;
}
