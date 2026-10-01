using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

internal sealed class ConditionPlanNode(
    string variableName,
    PlanNode whenFalse,
    PlanNode whenTrue,
    CostAnalyses analyses) : PlanNode
{
    public override bool DependsOnVariables => true;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
    {
        if (variableValues is null)
        {
            return PlanArithmetic.Join(
                whenFalse.Evaluate(null),
                whenTrue.Evaluate(null),
                analyses);
        }

        var value = variableValues.TryGetValue(variableName, out var node)
            && node is BooleanValueNode boolean
            && boolean.Value;
        return (value ? whenTrue : whenFalse).Evaluate(variableValues);
    }
}
