using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, thread-safe analysis plan for one operation and schema.
/// Each evaluation uses the supplied <see cref="IAnalysisAlgebra{TSummary}"/>.
/// </summary>
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
    /// Gets whether compilation exhausted <see cref="CostSchemaIndexOptions.CaseBudget"/>.
    /// When exhausted, evaluations may overestimate the result.
    /// This property is independent of the algebra supplied for evaluation.
    /// </summary>
    public bool HitCaseBudget { get; }

    /// <summary>
    /// Evaluates the analysis with coerced variable values for Boolean conditions and inherited
    /// list sizes. The supplied algebra is responsible for direct slicing arguments and input
    /// values. Construct built-in algebras with the same <paramref name="variables"/> to include
    /// those values in the result.
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
    /// Evaluates the analysis under schema assumptions, using
    /// <see cref="IAnalysisAlgebra{TSummary}.Join"/> to cover both outcomes of Boolean variables.
    /// Actual request values can produce a larger result.
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
    /// Gets a Boolean variable's value. Undefined or non-Boolean values are treated as <see langword="false"/>.
    /// </summary>
    private static bool ResolveBooleanVariable(ICostVariableValues variables, string variableName)
        => variables.TryGetValue(variableName, out var value) && value is BooleanValueNode { Value: true };
}
