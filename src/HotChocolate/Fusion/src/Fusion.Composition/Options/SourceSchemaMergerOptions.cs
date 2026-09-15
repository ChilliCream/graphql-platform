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
    /// Gets or sets the list size an executor MUST assume for a list field when no other
    /// list-size information applies (no supplied slicing argument, no
    /// <c>slicingArgumentDefaultValue</c>, no <c>assumedSize</c>, and no inherited sized field).
    /// <see langword="null"/> by default, meaning unbounded: no <c>@fusion__cost_options</c>
    /// directive is emitted, and the derived <c>@listSize</c> for a field with at least one
    /// unannotated serving source omits <c>assumedSize</c>.
    /// A non-null value must be non-negative.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is negative.
    /// </exception>
    public int? DefaultListSize
    {
        get;
        set
        {
            if (value is { } size && size < 0)
            {
                throw ThrowHelper.InvalidDefaultListSize(size);
            }

            field = value;
        }
    }

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
    /// Removes types and directives that are not referenced in the final merged schema.
    /// </summary>
    public bool RemoveUnreferencedDefinitions { get; set; } = true;

    /// <summary>
    /// Defines how to handle <c>@tag</c> directives when merging source schemas.
    /// </summary>
    public DirectiveMergeBehavior TagMergeBehavior { get; set; } = DirectiveMergeBehavior.IncludePrivate;
}
