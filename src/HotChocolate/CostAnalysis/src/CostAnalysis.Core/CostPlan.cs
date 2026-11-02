using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, thread-safe cost plan for one operation and schema.
/// </summary>
/// <remarks>
/// When compilation exhausts <see cref="CostSchemaIndexOptions.CaseBudget"/>,
/// <see cref="CostSchemaIndexOptions.CaseBudgetExceededBehavior"/> determines whether
/// requests receive exact costs or upper bounds for the remaining part of the operation.
/// </remarks>
public sealed class CostPlan
{
    private readonly PlanNode? _root;
    private readonly CostSchemaIndex? _schemaIndex;
    private readonly IReadOnlyDictionary<string, FragmentDefinitionNode>? _fragments;
    private readonly ConditionTree? _tree;
    private readonly CostAnalyses _analyses;
    private readonly bool _hitCaseBudget;
    private readonly CostEstimate _assumedBound;
    private readonly Func<CostEstimate>? _evaluateAssumedBoundEnvelope;
    private CostEstimate _lazyAssumedBound;
    private bool _lazyAssumedBoundComputed;
    private object? _lazyAssumedBoundGate;

    /// <summary>
    /// Creates a cost plan that is exact or includes upper bounds for parts that exceeded the case budget.
    /// </summary>
    internal CostPlan(PlanNode root, CostAnalyses analyses, bool hitCaseBudget)
    {
        _root = root;
        _analyses = analyses;
        _hitCaseBudget = hitCaseBudget;
        _assumedBound = root.Evaluate(variableValues: null);
    }

    /// <summary>
    /// Creates a plan that evaluates each request's exact cost after compilation exhausts the case budget.
    /// </summary>
    internal CostPlan(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        CostAnalyses analyses)
    {
        _schemaIndex = schemaIndex;
        _fragments = fragments;
        _tree = tree;
        _analyses = analyses;
        _hitCaseBudget = true;
        _lazyAssumedBoundGate = new object();

        // Reuse the delegate to avoid allocating it on each assumed-bound evaluation.
        _evaluateAssumedBoundEnvelope = EvaluateAssumedBoundEnvelope;
    }

    /// <summary>
    /// Gets the analyses this plan evaluates.
    /// </summary>
    public CostAnalyses Analyses => _analyses;

    /// <summary>
    /// Gets a value indicating whether this plan's estimate depends on
    /// coerced variable values.
    /// </summary>
    public bool DependsOnVariables => _root?.DependsOnVariables ?? true;

    /// <summary>
    /// Gets whether compilation exhausted <see cref="CostSchemaIndexOptions.CaseBudget"/>.
    /// <see cref="CostSchemaIndexOptions.CaseBudgetExceededBehavior"/> determines whether
    /// the plan evaluates exact costs or upper bounds.
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
        return _root is not null ? _root.Evaluate(variables) : EvaluatePerRequest(variables);
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
    public CostEstimate EvaluateAssumedBound()
        => _root is not null
            ? _assumedBound
            : LazyInitializer.EnsureInitialized(
                ref _lazyAssumedBound,
                ref _lazyAssumedBoundComputed,
                ref _lazyAssumedBoundGate,
                _evaluateAssumedBoundEnvelope!);

    /// <summary>
    /// Computes an upper bound under schema assumptions for a plan that exceeded the compilation budget.
    /// </summary>
    private CostEstimate EvaluateAssumedBoundEnvelope()
    {
        var algebra = new PerRequestCostAlgebra(_schemaIndex!, _analyses, variableValues: null);
        var budget = new CaseBudget(_schemaIndex!.CaseBudget);
        var decision = ExactCasesTraversal.Evaluate(
            _schemaIndex,
            _fragments!,
            _tree!,
            algebra,
            variableValues: null,
            budget);
        return decision.FoldWithJoin(algebra.Join);
    }

    /// <summary>
    /// Evaluates the operation's exact cost using the request's coerced variable values.
    /// </summary>
    private CostEstimate EvaluatePerRequest(ICostVariableValues variables)
    {
        var algebra = new PerRequestCostAlgebra(_schemaIndex!, _analyses, variables);
        var budget = new CaseBudget(_schemaIndex!.CaseBudget);
        var decision = ExactCasesTraversal.Evaluate(
            _schemaIndex,
            _fragments!,
            _tree!,
            algebra,
            variables,
            budget,
            resolveVariables: true);
        return decision.Resolve(variableName => ResolveBooleanVariable(variables, variableName));
    }

    /// <summary>
    /// Gets a Boolean variable's value. Undefined or non-Boolean values are treated as <see langword="false"/>.
    /// </summary>
    private static bool ResolveBooleanVariable(ICostVariableValues variables, string variableName)
        => variables.TryGetValue(variableName, out var value) && value is BooleanValueNode { Value: true };
}
