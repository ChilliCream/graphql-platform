using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Evaluates operations for traversal tests using <see cref="TestCostAlgebra"/> or a supplied algebra.
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
        var schemaIndex = ConditionTreeTestHelpers.BuildSchemaIndex(sdl);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(schemaIndex, document, operation, "Query");
        return ExactCasesTraversal.Evaluate(
            schemaIndex,
            fragments,
            tree,
            algebra,
            variableValues: null,
            new CaseBudget(caseBudget));
    }
}
