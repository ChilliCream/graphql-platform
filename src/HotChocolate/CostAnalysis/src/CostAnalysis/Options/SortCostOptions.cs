namespace HotChocolate.CostAnalysis;

/// <summary>
/// Represents the cost options for sorting.
/// </summary>
public sealed class SortCostOptions
{
    /// <summary>
    /// Gets or sets the default cost for a sort argument.
    /// </summary>
    public double? DefaultSortArgumentCost { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the default cost for a sort operation.
    /// </summary>
    public double? DefaultSortOperationCost { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets multiplier when a variable is used for the sort argument.
    /// </summary>
    public int? VariableMultiplier { get; set; } = 5;
}
