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
    [Obsolete(
        "The variable multiplier is retired. The cost analyzer evaluates coerced "
        + "variable values directly and no longer needs a compensating multiplier.",
        error: true)]
    public int? VariableMultiplier { get; set; } = 5;
}
