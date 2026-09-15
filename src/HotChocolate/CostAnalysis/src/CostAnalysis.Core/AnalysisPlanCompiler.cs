using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Compiles an <see cref="AnalysisPlan"/> for one operation against a
/// <see cref="CostSchemaSnapshot"/>. Compilation is algebra-independent: the
/// returned plan evaluates any <see cref="IAnalysisAlgebra{TSummary}"/>
/// supplied when it is later evaluated, unlike
/// <see cref="CostPlanCompiler.Compile"/>, which compiles one fixed set of
/// built-in analyses into an optimized slot tree.
/// </summary>
/// <example>
/// A custom algebra counts fields instead of pricing them, and runs through
/// the same compiled plan as any built-in analysis:
/// <code>
/// public sealed class FieldCountAlgebra : IAnalysisAlgebra&lt;int&gt;
/// {
///     public int Empty =&gt; 0;
///
///     public int Field(in CollectedFieldGroup group, int child) =&gt; 1 + child;
///
///     public int Combine(int left, int right) =&gt; left + right;
///
///     public int Join(int left, int right) =&gt; Math.Max(left, right);
///
///     public int Root(double rootTypeWeight, int selection) =&gt; selection;
/// }
///
/// var plan = AnalysisPlanCompiler.Compile(snapshot, document, operation);
/// var fieldCount = plan.Evaluate(new FieldCountAlgebra(), variables);
/// </code>
/// </example>
[Experimental(CostExperiments.AnalysisAlgebra)]
public static class AnalysisPlanCompiler
{
    /// <summary>
    /// Compiles an analysis plan for <paramref name="operation"/>.
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
    /// <returns>
    /// The compiled analysis plan.
    /// </returns>
    public static AnalysisPlan Compile(
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operation);

        var rootTypeName = snapshot.GetOperationTypeName(operation.Operation);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            rootTypeName);
        var budget = new CaseBudget(snapshot.CaseBudget);
        _ = ExactCasesTraversal.Evaluate(
            snapshot,
            fragments,
            tree,
            CaseBudgetProbeAlgebra.Instance,
            variableValues: null,
            budget);

        return new AnalysisPlan(snapshot, fragments, tree, budget.IsExhausted);
    }
}
