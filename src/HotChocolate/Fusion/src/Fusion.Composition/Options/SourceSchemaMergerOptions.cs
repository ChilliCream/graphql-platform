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
    /// Gets or sets the default assumed size for list fields without more specific
    /// list-size information. The default is <see langword="null"/>, which represents
    /// an unbounded list size.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned value is negative.
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
    /// Gets or sets the maximum allowed field cost. The value must be a non-negative finite number.
    /// </summary>
    public double? MaxFieldCost
    {
        get;
        set
        {
            if (value is { } cost && (!double.IsFinite(cost) || cost < 0))
            {
                throw ThrowHelper.InvalidMaxFieldCost(cost);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowed type cost. The value must be a non-negative finite number.
    /// </summary>
    public double? MaxTypeCost
    {
        get;
        set
        {
            if (value is { } cost && (!double.IsFinite(cost) || cost < 0))
            {
                throw ThrowHelper.InvalidMaxTypeCost(cost);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets whether to enforce configured cost limits.
    /// </summary>
    public bool? EnforceCostLimits { get; set; }

    /// <summary>
    /// Gets or sets whether to skip cost analysis.
    /// </summary>
    public bool? SkipCostAnalyzer { get; set; }

    /// <summary>
    /// Gets or sets the maximum estimated response size. The value must be a non-negative finite number.
    /// </summary>
    public double? MaxResponseSize
    {
        get;
        set
        {
            if (value is { } size && (!double.IsFinite(size) || size < 0))
            {
                throw ThrowHelper.InvalidMaxResponseSize(size);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of cached cost plans. The value must be positive.
    /// </summary>
    public int? CostPlanCacheSize
    {
        get;
        set
        {
            if (value is { } size && size <= 0)
            {
                throw ThrowHelper.InvalidCostPlanCacheSize(size);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of Boolean case splits allowed when compiling an operation.
    /// </summary>
    public int? CaseBudget { get; set; }

    /// <summary>
    /// Gets or sets the behavior when compiling an operation exhausts the case budget.
    /// </summary>
    public CaseBudgetExceededBehavior? CaseBudgetExceededBehavior { get; set; }

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
