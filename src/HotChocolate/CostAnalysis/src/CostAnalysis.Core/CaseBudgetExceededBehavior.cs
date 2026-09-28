namespace HotChocolate.CostAnalysis;

/// <summary>
/// Specifies how a cost plan behaves when compilation exhausts
/// <see cref="CostSchemaIndexOptions.CaseBudget"/>.
/// </summary>
public enum CaseBudgetExceededBehavior
{
    /// <summary>
    /// Evaluates the exact cost for each request using its coerced variable values.
    /// <see cref="CostPlan.HitCaseBudget"/> remains <see langword="true"/>, and
    /// <see cref="CostPlan.EvaluateAssumedBound"/> may overestimate the cost under schema assumptions.
    /// </summary>
    EvaluatePerRequest,

    /// <summary>
    /// Uses an upper bound for the part of the operation that exceeds the compilation budget.
    /// That part contributes the same estimate for every request.
    /// </summary>
    Overestimate
}
