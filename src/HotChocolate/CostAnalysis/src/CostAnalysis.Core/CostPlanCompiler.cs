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
        _ = snapshot;
        _ = document;
        _ = operation;
        _ = analyses;
        throw ThrowHelper.NotImplemented();
    }
}
