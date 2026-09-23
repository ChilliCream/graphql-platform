using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes IBM field and type costs together with maximum response size.
/// </summary>
public sealed class TupledAlgebra : IAnalysisAlgebra<CostEstimate>
{
    private readonly CostAlgebra _cost;
    private readonly ResponseSizeAlgebra _responseSize;

    /// <summary>
    /// Creates a combined cost and response-size analysis that uses schema assumptions for variable values.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index used by both analyses.
    /// </param>
    public TupledAlgebra(CostSchemaIndex schemaIndex)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        _cost = new CostAlgebra(schemaIndex);
        _responseSize = new ResponseSizeAlgebra(schemaIndex);
    }

    /// <summary>
    /// Creates a combined cost and response-size analysis that uses the request's coerced variable values.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index used by both analyses.
    /// </param>
    /// <param name="variableValues">
    /// The coerced variable values of the request.
    /// </param>
    [Experimental(CostExperiments.AnalysisAlgebra)]
    public TupledAlgebra(CostSchemaIndex schemaIndex, ICostVariableValues variableValues)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(variableValues);
        _cost = new CostAlgebra(schemaIndex, variableValues);
        _responseSize = new ResponseSizeAlgebra(schemaIndex, variableValues);
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
