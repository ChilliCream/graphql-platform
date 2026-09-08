namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, schema-scoped, thread-safe compiled cost plan for one
/// operation. Condition trees, type regions and constants are folded at
/// compile time; anything that depends on coerced variable values survives
/// as an evaluable slot.
/// </summary>
public sealed class CostPlan
{
    /// <summary>
    /// Gets the analyses this plan evaluates.
    /// </summary>
    public CostAnalyses Analyses => throw ThrowHelper.NotImplemented();

    /// <summary>
    /// Gets a value indicating whether this plan's estimate depends on
    /// coerced variable values.
    /// </summary>
    public bool DependsOnVariables => throw ThrowHelper.NotImplemented();

    /// <summary>
    /// Gets a value indicating whether compilation exhausted the
    /// <see cref="CostEngineOptions.CaseBudget"/>, so this plan's estimate
    /// is a sound but conservative fallback bound rather than the exact
    /// result.
    /// </summary>
    public bool HitCaseBudget => throw ThrowHelper.NotImplemented();

    /// <summary>
    /// Evaluates this plan against coerced variable values.
    /// </summary>
    /// <param name="variables">
    /// The coerced variable values of the request.
    /// </param>
    /// <returns>
    /// The resulting cost estimate.
    /// </returns>
    public CostEstimate Evaluate(ICostVariableValues variables)
    {
        _ = variables;
        throw ThrowHelper.NotImplemented();
    }

    /// <summary>
    /// Evaluates this plan's worst-case bound without coerced variable
    /// values.
    /// </summary>
    /// <returns>
    /// The static bound.
    /// </returns>
    public CostEstimate EvaluateStaticBound() => throw ThrowHelper.NotImplemented();
}
