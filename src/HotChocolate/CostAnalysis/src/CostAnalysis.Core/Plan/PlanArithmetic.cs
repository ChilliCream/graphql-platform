namespace HotChocolate.CostAnalysis;

internal static class PlanArithmetic
{
    public static CostEstimate Empty(CostAnalyses analyses)
        => new(0.0, 0.0, (analyses & CostAnalyses.ResponseSize) != 0 ? 0.0 : null);

    public static CostEstimate Combine(CostEstimate left, CostEstimate right, CostAnalyses analyses)
        => new(
            (analyses & CostAnalyses.Cost) != 0 ? left.FieldCost + right.FieldCost : 0.0,
            (analyses & CostAnalyses.Cost) != 0 ? left.TypeCost + right.TypeCost : 0.0,
            (analyses & CostAnalyses.ResponseSize) != 0
                ? ResponseSizeFieldRule.Combine(left.MaxResponseSize ?? 0.0, right.MaxResponseSize ?? 0.0)
                : null);

    public static CostEstimate Join(CostEstimate left, CostEstimate right, CostAnalyses analyses)
        => new(
            (analyses & CostAnalyses.Cost) != 0 ? double.MaxNumber(left.FieldCost, right.FieldCost) : 0.0,
            (analyses & CostAnalyses.Cost) != 0 ? double.MaxNumber(left.TypeCost, right.TypeCost) : 0.0,
            (analyses & CostAnalyses.ResponseSize) != 0
                ? ResponseSizeFieldRule.Join(left.MaxResponseSize ?? 0.0, right.MaxResponseSize ?? 0.0)
                : null);

    public static CostEstimate Root(double rootTypeWeight, CostEstimate selection, CostAnalyses analyses)
        => new(
            selection.FieldCost,
            (analyses & CostAnalyses.Cost) != 0
                ? CostFieldRule.Clamp0(rootTypeWeight + selection.TypeCost)
                : 0.0,
            selection.MaxResponseSize);
}
