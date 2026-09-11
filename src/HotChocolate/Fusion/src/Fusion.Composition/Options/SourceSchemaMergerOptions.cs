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
    public DirectiveMergeBehavior CacheControlMergeBehavior { get; set; } = DirectiveMergeBehavior.Include;

    /// <summary>
    /// Enables the inclusion of Global Object Identification fields.
    /// </summary>
    public bool EnableGlobalObjectIdentification { get; set; }

    /// <summary>
    /// Defines how enum values are merged when the same enum type is defined in multiple source
    /// schemas.
    /// </summary>
    public EnumValuesMergeBehavior EnumValuesMergeBehavior { get; set; } = EnumValuesMergeBehavior.Auto;

    /// <summary>
    /// Defines how the gateway resolves the <c>Query.node</c> field.
    /// </summary>
    public NodeResolution NodeResolution { get; set; } = NodeResolution.Gateway;

    /// <summary>
    /// Defines the consequence applied to a <c>@policy</c> application when its expression
    /// denies access and no source schema contributed an explicit <c>onDenied</c> value for it.
    /// </summary>
    public PolicyDenialBehavior PolicyOnDeniedDefault { get; set; } = PolicyDenialBehavior.Null;

    /// <summary>
    /// Removes types and directives that are not referenced in the final merged schema.
    /// </summary>
    public bool RemoveUnreferencedDefinitions { get; set; } = true;

    /// <summary>
    /// Defines how to handle <c>@tag</c> directives when merging source schemas.
    /// </summary>
    public DirectiveMergeBehavior TagMergeBehavior { get; set; } = DirectiveMergeBehavior.IncludePrivate;
}
