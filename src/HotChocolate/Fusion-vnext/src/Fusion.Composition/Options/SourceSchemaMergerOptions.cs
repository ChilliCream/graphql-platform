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
    /// Defines how to handle <c>@cacheControl</c> directives when merging source schemas.
    /// </summary>
    public DirectiveMergeBehavior CacheControlMergeBehavior { get; set; }

    /// <summary>
    /// Enables the inclusion of Global Object Identification fields.
    /// </summary>
    public bool EnableGlobalObjectIdentification { get; set; }

    /// <summary>
    /// Removes types and directives that are not referenced in the final merged schema.
    /// </summary>
    public bool RemoveUnreferencedDefinitions { get; set; } = true;

    /// <summary>
    /// Defines how to handle <c>@tag</c> directives when merging source schemas.
    /// </summary>
    public DirectiveMergeBehavior TagMergeBehavior { get; set; } = DirectiveMergeBehavior.IncludePrivate;
}
