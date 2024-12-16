namespace HotChocolate.CostAnalysis;

/// <summary>
/// Request options for cost analysis.
/// </summary>
/// <param name="MaxFieldCost">
/// The maximum allowed field cost.
/// </param>
/// <param name="MaxTypeCost">
/// The maximum allowed type cost.
/// </param>
/// <param name="EnforceCostLimits">
/// Defines if the analyzer shall enforce cost limits.
/// </param>
/// <param name="FilterVariableMultiplier">
/// The filter variable multiplier.
/// </param>
public record RequestCostOptions(
    double MaxFieldCost,
    double MaxTypeCost,
    bool EnforceCostLimits,
    int? FilterVariableMultiplier);
