namespace HotChocolate.CostAnalysis;

/// <summary>
/// Options for cost analysis.
/// </summary>
public sealed class CostOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed field cost.
    /// </summary>
    public double MaxFieldCost { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets the maximum allowed type cost.
    /// </summary>
    public double MaxTypeCost { get; set; } = 1_000;

    /// <summary>
    /// Defines if the analyzer shall enforce cost limits.
    /// </summary>
    public bool EnforceCostLimits { get; set; } = true;

    /// <summary>
    /// Defines if cost defaults shall be applied to the schema.
    /// </summary>
    public bool ApplyCostDefaults { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cost for an async resolver pipeline.
    /// </summary>
    public double? DefaultResolverCost { get; set; } = 10.0;

    /// <summary>
    /// Gets the cost defaults for filtering.
    /// </summary>
    public FilterCostOptions Filtering { get; } = new();

    /// <summary>
    /// Gets the cost defaults for sorting.
    /// </summary>
    public SortCostOptions Sorting { get; } = new();
}
