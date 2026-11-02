namespace HotChocolate.Fusion.Options;

/// <summary>
/// Defines how cost analysis proceeds when compilation exhausts the case budget.
/// </summary>
public enum CaseBudgetExceededBehavior
{
    /// <summary>
    /// Evaluates cost for each request using its variable values.
    /// </summary>
    EvaluatePerRequest,

    /// <summary>
    /// Uses an upper-bound estimate after the case budget is exhausted.
    /// </summary>
    Overestimate
}
