namespace HotChocolate.CostAnalysis;

/// <summary>
/// One immutable node in a compiled cost-plan expression.
/// </summary>
internal abstract class PlanNode
{
    public abstract bool DependsOnVariables { get; }

    public abstract CostEstimate Evaluate(ICostVariableValues? variableValues);

    public static PlanNode Constant(CostEstimate value) => new ConstantPlanNode(value);

    public static PlanNode Combine(PlanNode left, PlanNode right, CostAnalyses analyses)
        => FoldBinary(left, right, analyses, isJoin: false);

    public static PlanNode Join(PlanNode left, PlanNode right, CostAnalyses analyses)
        => FoldBinary(left, right, analyses, isJoin: true);

    public static PlanNode Root(double rootTypeWeight, PlanNode selection, CostAnalyses analyses)
    {
        if (!selection.DependsOnVariables)
        {
            return Constant(PlanArithmetic.Root(rootTypeWeight, selection.Evaluate(null), analyses));
        }

        return new RootPlanNode(rootTypeWeight, selection, analyses);
    }

    public static PlanNode Condition(
        string variableName,
        PlanNode whenFalse,
        PlanNode whenTrue,
        CostAnalyses analyses)
    {
        if (whenFalse is ConstantPlanNode falseConstant
            && whenTrue is ConstantPlanNode trueConstant
            && falseConstant.Value == trueConstant.Value)
        {
            return falseConstant;
        }

        return new ConditionPlanNode(variableName, whenFalse, whenTrue, analyses);
    }

    private static PlanNode FoldBinary(
        PlanNode left,
        PlanNode right,
        CostAnalyses analyses,
        bool isJoin)
    {
        if (!left.DependsOnVariables && !right.DependsOnVariables)
        {
            var leftValue = left.Evaluate(null);
            var rightValue = right.Evaluate(null);
            return Constant(isJoin
                ? PlanArithmetic.Join(leftValue, rightValue, analyses)
                : PlanArithmetic.Combine(leftValue, rightValue, analyses));
        }

        return new BinaryPlanNode(left, right, analyses, isJoin);
    }
}
