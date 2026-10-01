namespace HotChocolate.CostAnalysis;

internal sealed class BinaryPlanNode(
    PlanNode left,
    PlanNode right,
    CostAnalyses analyses,
    bool isJoin) : PlanNode
{
    public override bool DependsOnVariables => left.DependsOnVariables || right.DependsOnVariables;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
    {
        var leftValue = left.Evaluate(variableValues);
        var rightValue = right.Evaluate(variableValues);
        return isJoin
            ? PlanArithmetic.Join(leftValue, rightValue, analyses)
            : PlanArithmetic.Combine(leftValue, rightValue, analyses);
    }
}
