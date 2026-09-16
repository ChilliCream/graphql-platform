using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, schema-scoped, thread-safe compiled analysis plan for one
/// operation, produced by <see cref="AnalysisPlanCompiler.Compile"/>.
/// Unlike <see cref="CostPlan"/>, this plan is algebra-independent: it holds
/// only the operation's condition tree and fragments, and evaluates any
/// <see cref="IAnalysisAlgebra{TSummary}"/> supplied at call time rather
/// than one fixed set of analyses compiled into slots.
/// </summary>
/// <remarks>
/// Every <see cref="Evaluate{TSummary}"/> and
/// <see cref="EvaluateAssumedBound{TSummary}"/> call re-runs the ExactCases
/// traversal for the supplied algebra, unlike <see cref="CostPlan"/>'s
/// compiled slot tree.
/// </remarks>
[Experimental(CostExperiments.AnalysisAlgebra)]
public sealed class AnalysisPlan
{
    private readonly CostSchemaIndex _schemaIndex;
    private readonly IReadOnlyDictionary<string, FragmentDefinitionNode> _fragments;
    private readonly ConditionTree _tree;

    internal AnalysisPlan(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        bool hitCaseBudget)
    {
        _schemaIndex = schemaIndex;
        _fragments = fragments;
        _tree = tree;
        HitCaseBudget = hitCaseBudget;
    }

    /// <summary>
    /// Gets a value indicating whether compilation exhausted the schema's
    /// case budget (<c>CostSchemaIndex.CaseBudget</c>), so every
    /// evaluation of this plan falls back to the conservative envelope
    /// bound rather than the exact result. This outcome depends only on the
    /// operation's condition-tree shape and the schema's case budget, never
    /// on the algebra an evaluation is called with.
    /// </summary>
    public bool HitCaseBudget { get; }

    /// <summary>
    /// Evaluates <paramref name="algebra"/> against coerced variable
    /// values: a Boolean <c>@include</c>/<c>@skip</c> variable resolves from
    /// its coerced value, and a <c>@listSize(sizedFields:)</c> inherited
    /// size (<see cref="CollectedFieldGroup.InheritedSize"/>) resolves
    /// against <paramref name="variables"/> as well. A field's own direct
    /// slicing arguments and input-value pricing are resolved by
    /// <paramref name="algebra"/> itself: the built-in
    /// <see cref="CostAlgebra"/>, <see cref="ResponseSizeAlgebra"/> and
    /// <see cref="TupledAlgebra"/> only receive <paramref name="variables"/>
    /// for that purpose when constructed with their
    /// <c>(CostSchemaIndex, ICostVariableValues)</c> overload; a custom
    /// algebra that resolves slicing or input values itself must do the
    /// same.
    /// </summary>
    /// <typeparam name="TSummary">
    /// The summary type <paramref name="algebra"/> combines and joins.
    /// </typeparam>
    /// <param name="algebra">
    /// The analysis algebra to evaluate.
    /// </param>
    /// <param name="variables">
    /// The coerced variable values of the request.
    /// </param>
    /// <returns>
    /// The resulting summary.
    /// </returns>
    public TSummary Evaluate<TSummary>(IAnalysisAlgebra<TSummary> algebra, ICostVariableValues variables)
    {
        ArgumentNullException.ThrowIfNull(algebra);
        ArgumentNullException.ThrowIfNull(variables);

        var decision = EvaluateDecision(algebra, variables);
        return decision.Resolve(variableName => ResolveBooleanVariable(variables, variableName));
    }

    /// <summary>
    /// Evaluates <paramref name="algebra"/> under the schema's assumptions:
    /// every still-open Boolean variable is folded with
    /// <see cref="IAnalysisAlgebra{TSummary}.Join"/> instead of resolved
    /// from a coerced value, the same assumed-bound contract
    /// <see cref="CostPlan.EvaluateAssumedBound"/> documents for the
    /// built-in cost algebra.
    /// </summary>
    /// <typeparam name="TSummary">
    /// The summary type <paramref name="algebra"/> combines and joins.
    /// </typeparam>
    /// <param name="algebra">
    /// The analysis algebra to evaluate.
    /// </param>
    /// <returns>
    /// The assumed bound.
    /// </returns>
    public TSummary EvaluateAssumedBound<TSummary>(IAnalysisAlgebra<TSummary> algebra)
    {
        ArgumentNullException.ThrowIfNull(algebra);

        var decision = EvaluateDecision(algebra, variableValues: null);
        return decision.FoldWithJoin(algebra.Join);
    }

    private BooleanDecision<TSummary> EvaluateDecision<TSummary>(
        IAnalysisAlgebra<TSummary> algebra,
        ICostVariableValues? variableValues)
    {
        var budget = new CaseBudget(_schemaIndex.CaseBudget);
        return ExactCasesTraversal.Evaluate(_schemaIndex, _fragments, _tree, algebra, variableValues, budget);
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
