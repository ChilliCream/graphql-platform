using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Evaluates IBM cost and maximum response-size together in one traversal.
/// </summary>
public sealed class TupledAlgebra : IAnalysisAlgebra<CostEstimate>
{
    private readonly CostAlgebra _cost;
    private readonly ResponseSizeAlgebra _responseSize;

    /// <summary>
    /// Initializes a new instance of <see cref="TupledAlgebra"/> for the
    /// static/assumed path: a variable-bound slicing argument or input value
    /// falls back to its schema-declared assumption instead of a coerced
    /// value.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot used by both analyses.
    /// </param>
    public TupledAlgebra(CostSchemaSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _cost = new CostAlgebra(snapshot);
        _responseSize = new ResponseSizeAlgebra(snapshot);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TupledAlgebra"/> that
    /// resolves a variable-bound slicing argument or input value from
    /// <paramref name="variableValues"/>, the same coerced values the
    /// optimized <see cref="CostPlan"/> path receives at evaluation time.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot used by both analyses.
    /// </param>
    /// <param name="variableValues">
    /// The coerced variable values of the request.
    /// </param>
    [Experimental(CostExperiments.AnalysisAlgebra)]
    public TupledAlgebra(CostSchemaSnapshot snapshot, ICostVariableValues variableValues)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(variableValues);
        _cost = new CostAlgebra(snapshot, variableValues);
        _responseSize = new ResponseSizeAlgebra(snapshot, variableValues);
    }

    /// <inheritdoc />
    public CostEstimate Empty => WithResponseSize(_cost.Empty, _responseSize.Empty);

    /// <inheritdoc />
    public CostEstimate Field(in CollectedFieldGroup group, CostEstimate child)
        => WithResponseSize(
            _cost.Field(group, WithoutResponseSize(child)),
            _responseSize.Field(group, child.MaxResponseSize ?? ResponseSizeFieldRule.Empty));

    /// <inheritdoc />
    public CostEstimate Combine(CostEstimate left, CostEstimate right)
        => WithResponseSize(
            _cost.Combine(WithoutResponseSize(left), WithoutResponseSize(right)),
            _responseSize.Combine(
                left.MaxResponseSize ?? ResponseSizeFieldRule.Empty,
                right.MaxResponseSize ?? ResponseSizeFieldRule.Empty));

    /// <inheritdoc />
    public CostEstimate Join(CostEstimate left, CostEstimate right)
        => WithResponseSize(
            _cost.Join(WithoutResponseSize(left), WithoutResponseSize(right)),
            _responseSize.Join(
                left.MaxResponseSize ?? ResponseSizeFieldRule.Empty,
                right.MaxResponseSize ?? ResponseSizeFieldRule.Empty));

    /// <inheritdoc />
    public CostEstimate Root(double rootTypeWeight, CostEstimate selection)
        => WithResponseSize(
            _cost.Root(rootTypeWeight, WithoutResponseSize(selection)),
            _responseSize.Root(rootTypeWeight, selection.MaxResponseSize ?? ResponseSizeFieldRule.Empty));

    private static CostEstimate WithoutResponseSize(CostEstimate estimate)
        => new(estimate.FieldCost, estimate.TypeCost, null);

    private static CostEstimate WithResponseSize(CostEstimate estimate, double responseSize)
        => new(estimate.FieldCost, estimate.TypeCost, responseSize);
}
