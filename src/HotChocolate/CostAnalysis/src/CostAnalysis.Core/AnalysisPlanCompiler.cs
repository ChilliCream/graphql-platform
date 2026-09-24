using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Compiles an operation into an <see cref="AnalysisPlan"/> for a schema.
/// The plan accepts any <see cref="IAnalysisAlgebra{TSummary}"/> at evaluation time.
/// </summary>
[Experimental(CostExperiments.AnalysisAlgebra)]
public static class AnalysisPlanCompiler
{
    /// <summary>
    /// Compiles an analysis plan for <paramref name="operation"/>.
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
    /// <returns>
    /// The compiled analysis plan.
    /// </returns>
    public static AnalysisPlan Compile(
        CostSchemaIndex schemaIndex,
        DocumentNode document,
        OperationDefinitionNode operation)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operation);

        var rootTypeName = schemaIndex.GetOperationTypeName(operation.Operation);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            schemaIndex,
            document,
            operation,
            rootTypeName);
        var budget = new CaseBudget(schemaIndex.CaseBudget);
        _ = ExactCasesTraversal.Evaluate(
            schemaIndex,
            fragments,
            tree,
            CaseBudgetProbeAlgebra.Instance,
            variableValues: null,
            budget);

        return new AnalysisPlan(schemaIndex, fragments, tree, budget.IsExhausted);
    }
}
