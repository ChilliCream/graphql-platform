namespace HotChocolate.CostAnalysis;

internal sealed class ConstantPlanNode(CostEstimate value) : PlanNode
{
    public CostEstimate Value { get; } = value;

    public override bool DependsOnVariables => false;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues) => Value;
}
