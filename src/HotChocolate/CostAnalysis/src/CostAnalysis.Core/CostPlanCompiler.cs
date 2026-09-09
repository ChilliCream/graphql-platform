using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Compiles a <see cref="CostPlan"/> for one operation against a
/// <see cref="CostSchemaSnapshot"/>. Compilation consumes only
/// coerced-variable-independent information; everything variable-dependent
/// survives in the returned plan as an evaluable slot.
/// </summary>
public static class CostPlanCompiler
{
    /// <summary>
    /// Compiles a cost plan for <paramref name="operation"/>.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot to compile against.
    /// </param>
    /// <param name="document">
    /// The document that contains <paramref name="operation"/> and any
    /// fragments it spreads.
    /// </param>
    /// <param name="operation">
    /// The operation to compile.
    /// </param>
    /// <param name="analyses">
    /// The analyses the compiled plan evaluates.
    /// </param>
    /// <returns>
    /// The compiled cost plan.
    /// </returns>
    public static CostPlan Compile(
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation,
        CostAnalyses analyses)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operation);

        const CostAnalyses all = CostAnalyses.Cost | CostAnalyses.ResponseSize;

        if (analyses == 0 || (analyses & ~all) != 0)
        {
            throw ThrowHelper.InvalidAnalyses(analyses);
        }

        var rootTypeName = snapshot.GetOperationTypeName(operation.Operation);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            rootTypeName);
        var budget = new CaseBudget(snapshot.Options.CaseBudget);
        var algebra = new PlanAlgebra(snapshot, analyses);
        var decision = ExactCasesTraversal.Evaluate(snapshot, fragments, tree, algebra, budget);
        var root = CompileDecision(decision, analyses);

        return new CostPlan(root, analyses, budget.IsExhausted);
    }

    private static PlanNode CompileDecision(
        BooleanDecision<PlanNode> decision,
        CostAnalyses analyses)
        => decision switch
        {
            LeafDecision<PlanNode> leaf => leaf.Value,
            SplitDecision<PlanNode> split => PlanNode.Condition(
                split.Variable,
                CompileDecision(split.WhenFalse, analyses),
                CompileDecision(split.WhenTrue, analyses),
                analyses),
            _ => throw ThrowHelper.UnexpectedDecision()
        };
}
