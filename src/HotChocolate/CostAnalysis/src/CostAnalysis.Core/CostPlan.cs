using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, schema-scoped, thread-safe compiled cost plan for one
/// operation. Condition trees, type regions and constants are folded at
/// compile time; anything that depends on coerced variable values survives
/// as an evaluable slot.
/// </summary>
/// <remarks>
/// Once compiling one operation exhausts the schema's
/// <see cref="CostSchemaIndexOptions.CaseBudget"/>,
/// <see cref="CostSchemaIndexOptions.CaseBudgetExceededBehavior"/> decides
/// what this plan evaluates for the rest of its lifetime:
/// <see cref="CaseBudgetExceededBehavior.EvaluatePerRequest"/> discards the
/// partial compile and re-derives the exact result from the operation's
/// condition tree on every <see cref="Evaluate"/> call, while
/// <see cref="CaseBudgetExceededBehavior.Overestimate"/> bakes a
/// conservative envelope for the unaffordable remainder into the compiled
/// tree once, up front.
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
    /// Initializes a new instance of <see cref="CostPlan"/> for a compiled
    /// plan tree: either the operation's exact compile, or, under
    /// <see cref="CaseBudgetExceededBehavior.Overestimate"/>, a tree whose
    /// unaffordable remainder is a baked-in conservative envelope.
    /// </summary>
    internal CostPlan(PlanNode root, CostAnalyses analyses, bool hitCaseBudget)
    {
        _root = root;
        _analyses = analyses;
        _hitCaseBudget = hitCaseBudget;
        _assumedBound = root.Evaluate(variableValues: null);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CostPlan"/> for
    /// <see cref="CaseBudgetExceededBehavior.EvaluatePerRequest"/>: the
    /// compile is discarded, and <see cref="Evaluate"/> instead traverses
    /// <paramref name="tree"/> exactly, per request.
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

        // Cached once: the method-group-to-delegate conversion below would otherwise allocate a
        // fresh delegate on every EvaluateAssumedBound call, defeating the zero-allocation warm
        // path the cached decision depends on.
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
    /// Gets a value indicating whether compilation exhausted the
    /// <see cref="CostSchemaIndexOptions.CaseBudget"/>, so this plan's estimate
    /// is either a sound but conservative fallback bound or, under
    /// <see cref="CaseBudgetExceededBehavior.EvaluatePerRequest"/>, still the
    /// exact result, re-derived per request instead of compiled once.
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
    /// Runs the variable-free envelope traversal exactly once for a plan
    /// that discarded its compile: the same fallback envelope
    /// <see cref="CaseBudgetExceededBehavior.Overestimate"/> would have
    /// baked in, folded to one summary rather than kept as a tree.
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
    /// Traverses the operation's condition tree exactly, resolving every
    /// still-open Boolean variable from <paramref name="variables"/> rather
    /// than following both branches, under the schema's case budget as a
    /// backstop: resolve mode itself never spends it.
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
    /// Resolves one Boolean <c>@include</c>/<c>@skip</c> variable's coerced
    /// value, matching <see cref="ConditionPlanNode"/>'s coercion exactly:
    /// an undefined variable or a non-Boolean coerced value is treated as
    /// <see langword="false"/>.
    /// </summary>
    private static bool ResolveBooleanVariable(ICostVariableValues variables, string variableName)
        => variables.TryGetValue(variableName, out var value) && value is BooleanValueNode { Value: true };
}
