namespace HotChocolate.CostAnalysis;

/// <summary>
/// Compiles traversal operations into immutable plan nodes.
/// </summary>
internal sealed class PlanAlgebra(
    CostSchemaSnapshot snapshot,
    CostAnalyses analyses) : IAnalysisAlgebra<PlanNode>, IInheritedSizePlanAlgebra<PlanNode>
{
    public PlanNode Empty { get; } = PlanNode.Constant(PlanArithmetic.Empty(analyses));

    public PlanNode Field(in CollectedFieldGroup group, PlanNode child)
        => FoldField(new FieldPlanNode(snapshot, analyses, group, inheritedSizeContext: null, child));

    public PlanNode Field(
        in CollectedFieldGroup group,
        SizedFieldContext? inheritedSizeContext,
        PlanNode child)
        => FoldField(new FieldPlanNode(snapshot, analyses, group, inheritedSizeContext, child));

    public PlanNode Combine(PlanNode left, PlanNode right)
        => PlanNode.Combine(left, right, analyses);

    public PlanNode Join(PlanNode left, PlanNode right)
        => PlanNode.Join(left, right, analyses);

    public PlanNode Root(double rootTypeWeight, PlanNode selection)
        => PlanNode.Root(rootTypeWeight, selection, analyses);

    private static PlanNode FoldField(FieldPlanNode field)
        => field.DependsOnVariables
            ? field
            : PlanNode.Constant(field.Evaluate(variableValues: null));
}

/// <summary>
/// Receives unresolved inherited list-size metadata while compiling a plan.
/// </summary>
internal interface IInheritedSizePlanAlgebra<TSummary>
{
    TSummary Field(
        in CollectedFieldGroup group,
        SizedFieldContext? inheritedSizeContext,
        TSummary child);
}
