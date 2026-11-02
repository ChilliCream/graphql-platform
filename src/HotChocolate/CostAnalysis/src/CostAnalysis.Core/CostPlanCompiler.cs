using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Compiles an operation into a cost plan for a schema.
/// The plan accepts coerced variable values at evaluation time.
/// </summary>
public static class CostPlanCompiler
{
    /// <summary>
    /// Compiles a cost plan for <paramref name="operation"/>.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index to compile against.
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
        CostSchemaIndex schemaIndex,
        DocumentNode document,
        OperationDefinitionNode operation,
        CostAnalyses analyses)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operation);

        const CostAnalyses all = CostAnalyses.Cost | CostAnalyses.ResponseSize;

        if (analyses == 0 || (analyses & ~all) != 0)
        {
            throw ThrowHelper.InvalidAnalyses(analyses);
        }

        var rootTypeName = schemaIndex.GetOperationTypeName(operation.Operation);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            schemaIndex,
            document,
            operation,
            rootTypeName);
        var budget = new CaseBudget(schemaIndex.CaseBudget);
        var algebra = new PlanAlgebra(schemaIndex, analyses);
        var decision = ExactCasesTraversal.Evaluate(
            schemaIndex, fragments, tree, algebra, variableValues: null, budget);

        if (budget.IsExhausted
            && schemaIndex.CaseBudgetExceededBehavior == CaseBudgetExceededBehavior.EvaluatePerRequest)
        {
            // Re-evaluate per request to preserve exact costs after the compilation budget is exhausted.
            return new CostPlan(schemaIndex, fragments, tree, analyses);
        }

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
            JoinDecision<PlanNode> joined => PlanNode.Join(
                CompileDecision(joined.Left, analyses),
                CompileDecision(joined.Right, analyses),
                analyses),
            _ => throw ThrowHelper.UnexpectedDecision()
        };
}
