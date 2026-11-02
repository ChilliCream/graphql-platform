namespace HotChocolate.CostAnalysis;

internal sealed class RootPlanNode(
    double rootTypeWeight,
    PlanNode selection,
    CostAnalyses analyses) : PlanNode
{
    public override bool DependsOnVariables => selection.DependsOnVariables;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
        => PlanArithmetic.Root(rootTypeWeight, selection.Evaluate(variableValues), analyses);
}
