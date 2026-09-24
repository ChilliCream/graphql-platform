namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes a plan's configured analyses using coerced variable values or schema assumptions.
/// </summary>
internal sealed class PerRequestCostAlgebra : IAnalysisAlgebra<CostEstimate>
{
    private readonly CostAnalyses _analyses;
    private readonly CostAlgebra? _cost;
    private readonly ResponseSizeAlgebra? _responseSize;

    public PerRequestCostAlgebra(
        CostSchemaIndex schemaIndex,
        CostAnalyses analyses,
        ICostVariableValues? variableValues)
    {
        _analyses = analyses;
        _cost = (analyses & CostAnalyses.Cost) != 0
            ? CreateCostAlgebra(schemaIndex, variableValues)
            : null;
        _responseSize = (analyses & CostAnalyses.ResponseSize) != 0
            ? CreateResponseSizeAlgebra(schemaIndex, variableValues)
            : null;
    }

    public CostEstimate Empty => PlanArithmetic.Empty(_analyses);

    public CostEstimate Field(in CollectedFieldGroup group, CostEstimate child)
    {
        var value = PlanArithmetic.Empty(_analyses);

        if (_cost is not null)
        {
            var cost = _cost.Field(group, new CostEstimate(child.FieldCost, child.TypeCost, null));
            value = new CostEstimate(cost.FieldCost, cost.TypeCost, value.MaxResponseSize);
        }

        if (_responseSize is not null)
        {
            value = new CostEstimate(
                value.FieldCost,
                value.TypeCost,
                _responseSize.Field(group, child.MaxResponseSize ?? 0.0));
        }

        return value;
    }

    public CostEstimate Combine(CostEstimate left, CostEstimate right)
        => PlanArithmetic.Combine(left, right, _analyses);

    public CostEstimate Join(CostEstimate left, CostEstimate right)
        => PlanArithmetic.Join(left, right, _analyses);

    public CostEstimate Root(double rootTypeWeight, CostEstimate selection)
        => PlanArithmetic.Root(rootTypeWeight, selection, _analyses);

    private static CostAlgebra CreateCostAlgebra(
        CostSchemaIndex schemaIndex,
        ICostVariableValues? variableValues)
        => variableValues is null
            ? new CostAlgebra(schemaIndex)
            : new CostAlgebra(schemaIndex, variableValues);

    private static ResponseSizeAlgebra CreateResponseSizeAlgebra(
        CostSchemaIndex schemaIndex,
        ICostVariableValues? variableValues)
        => variableValues is null
            ? new ResponseSizeAlgebra(schemaIndex)
            : new ResponseSizeAlgebra(schemaIndex, variableValues);
}
