namespace HotChocolate.CostAnalysis;

/// <summary>
/// Specifies which analyses a compiled <see cref="CostPlan"/> evaluates.
/// </summary>
[Flags]
public enum CostAnalyses
{
    /// <summary>
    /// The IBM field/type cost analysis.
    /// </summary>
    Cost = 1,

    /// <summary>
    /// The maximum response size analysis.
    /// </summary>
    ResponseSize = 2
}
