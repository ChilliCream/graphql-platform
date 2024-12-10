namespace HotChocolate.CostAnalysis;

public record RequestCostOptions(
    double MaxFieldCost,
    double MaxTypeCost,
    bool EnforceCostLimits,
    int? FilterVariableMultiplier);
