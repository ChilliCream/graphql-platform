namespace HotChocolate.CostAnalysis;

/// <summary>
/// Represents the cost options for filtering.
/// </summary>
public sealed class FilterCostOptions
{
    /// <summary>
    /// Gets or sets the default cost for a filter argument.
    /// </summary>
    public double? DefaultFilterArgumentCost { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the default cost for a filter operation.
    /// </summary>
    public double? DefaultFilterOperationCost { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the default cost for an expensive filter argument.
    /// </summary>
    public double? DefaultExpensiveFilterOperationCost { get; set; } = 20.0;

    /// <summary>
    /// Gets or sets a multiplier when a variable is used for the filter argument.
    /// </summary>
    public int? VariableMultiplier { get; set; } = 5;
}
