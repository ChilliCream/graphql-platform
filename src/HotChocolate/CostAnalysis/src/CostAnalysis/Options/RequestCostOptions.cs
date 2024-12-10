namespace HotChocolate.CostAnalysis;

public record CostRequestOptions(
    double MaxFieldCost,
    double MaxTypeCost,
    bool EnforceCostLimits,
    int? FilterVariableMultiplier);
