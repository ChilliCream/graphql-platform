namespace HotChocolate.CostAnalysis;

/// <summary>
/// Selects how a compiled <see cref="CostPlan"/> behaves once compiling one
/// operation exhausts the schema's <see cref="CostSchemaIndexOptions.CaseBudget"/>.
/// </summary>
public enum CaseBudgetExceededBehavior
{
    /// <summary>
    /// Discards the partial compile and evaluates the operation's exact
    /// cost per request instead, by traversing its condition tree against
    /// the request's own coerced variable values. The plan still reports
    /// <see cref="CostPlan.HitCaseBudget"/> as <see langword="true"/>, and
    /// its assumed bound (<see cref="CostPlan.EvaluateAssumedBound"/>)
    /// remains a conservative envelope, computed once and cached.
    /// </summary>
    EvaluatePerRequest,

    /// <summary>
    /// Bakes a sound but conservative envelope bound for the remainder of
    /// the operation into the compiled plan: every request the plan
    /// evaluates shares the same conservative result for the part of the
    /// operation the budget could not afford to compile exactly.
    /// </summary>
    Overestimate
}
