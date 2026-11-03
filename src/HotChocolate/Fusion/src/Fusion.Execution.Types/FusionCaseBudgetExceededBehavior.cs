namespace HotChocolate.Fusion.Types;

/// <summary>
/// Defines how cost analysis proceeds when the schema case budget is exhausted.
/// </summary>
public enum FusionCaseBudgetExceededBehavior
{
    /// <summary>
    /// Evaluates the cost for each request using its variable values.
    /// </summary>
    EvaluatePerRequest,

    /// <summary>
    /// Uses an upper-bound estimate after the case budget is exhausted.
    /// </summary>
    Overestimate
}
