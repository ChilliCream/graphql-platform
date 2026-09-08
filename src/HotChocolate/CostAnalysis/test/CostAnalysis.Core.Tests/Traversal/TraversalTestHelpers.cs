using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Shared helpers for the Traversal test suite: extracting an operation's
/// root condition tree and running <see cref="ExactCasesTraversal"/> over it
/// against a <see cref="TestCostAlgebra"/>.
/// </summary>
internal static class TraversalTestHelpers
{
    /// <summary>
    /// Extracts and evaluates <paramref name="operationText"/>'s root
    /// boundary against a schema built from <paramref name="sdl"/>.
    /// </summary>
    public static BooleanDecision<(double TypeCost, double FieldCost)> EvaluateOperation(
        string sdl,
        string operationText,
        int caseBudget = 4096)
        => EvaluateOperation(sdl, operationText, new TestCostAlgebra(), caseBudget);

    /// <summary>
    /// Extracts and evaluates <paramref name="operationText"/>'s root
    /// boundary against a schema built from <paramref name="sdl"/>, using
    /// the given algebra.
    /// </summary>
    public static BooleanDecision<TSummary> EvaluateOperation<TSummary>(
        string sdl,
        string operationText,
        IAnalysisAlgebra<TSummary> algebra,
        int caseBudget = 4096)
    {
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(sdl);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        return ExactCasesTraversal.Evaluate(snapshot, fragments, tree, algebra, new CaseBudget(caseBudget));
    }
}
