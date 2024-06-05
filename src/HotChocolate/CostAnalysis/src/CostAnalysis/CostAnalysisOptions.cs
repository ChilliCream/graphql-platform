namespace HotChocolate.CostAnalysis;

public sealed class CostAnalysisOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed field cost.
    /// </summary>
    public double MaxFieldCost { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets the maximum allowed type cost.
    /// </summary>
    public double MaxTypeCost { get; set; } = 1_000;
}
