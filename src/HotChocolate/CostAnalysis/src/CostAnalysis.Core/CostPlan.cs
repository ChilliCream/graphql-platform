namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, schema-scoped, thread-safe compiled cost plan for one
/// operation. Condition trees, type regions and constants are folded at
/// compile time; anything that depends on coerced variable values survives
/// as an evaluable slot.
/// </summary>
public sealed class CostPlan
{
    private readonly PlanNode _root;
    private readonly CostEstimate _assumedBound;
    private readonly CostAnalyses _analyses;
    private readonly bool _hitCaseBudget;

    /// <summary>
    /// Initializes a new instance of <see cref="CostPlan"/>.
    /// </summary>
    internal CostPlan(PlanNode root, CostAnalyses analyses, bool hitCaseBudget)
    {
        _root = root;
        _analyses = analyses;
        _hitCaseBudget = hitCaseBudget;
        _assumedBound = root.Evaluate(variableValues: null);
    }

    /// <summary>
    /// Gets the analyses this plan evaluates.
    /// </summary>
    public CostAnalyses Analyses => _analyses;

    /// <summary>
    /// Gets a value indicating whether this plan's estimate depends on
    /// coerced variable values.
    /// </summary>
    public bool DependsOnVariables => _root.DependsOnVariables;

    /// <summary>
    /// Gets a value indicating whether compilation exhausted the
    /// <see cref="CostSchemaIndexOptions.CaseBudget"/>, so this plan's estimate
    /// is a sound but conservative fallback bound rather than the exact
    /// result.
    /// </summary>
    public bool HitCaseBudget => _hitCaseBudget;

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
        ArgumentNullException.ThrowIfNull(variables);
        return _root.Evaluate(variables);
    }

    /// <summary>
    /// Evaluates the plan under the schema's assumptions: variable-bound
    /// slicing arguments resolve to <c>assumedSize</c> (else the default
    /// list size), Boolean variables to the more expensive branch, and
    /// variable-supplied input lists to one element. A request's evaluated
    /// cost can exceed this value.
    /// </summary>
    /// <returns>
    /// The assumed bound.
    /// </returns>
    public CostEstimate EvaluateAssumedBound() => _assumedBound;
}
