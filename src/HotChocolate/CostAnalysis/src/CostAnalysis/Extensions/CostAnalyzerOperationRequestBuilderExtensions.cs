namespace HotChocolate.Execution;

/// <summary>
/// Cost Analyzer extensions for the <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class CostAnalyzerOperationRequestBuilderExtensions
{
    public static OperationRequestBuilder ReportCost(this OperationRequestBuilder builder)
    {
        builder.RemoveGlobalState(WellKnownContextData.ValidateCost);
        return builder.AddGlobalState(WellKnownContextData.ReporCostInResponse, true);
    }

    public static OperationRequestBuilder ValidateCost(this OperationRequestBuilder builder)
    {
        builder.RemoveGlobalState(WellKnownContextData.ReporCostInResponse);
        return builder.AddGlobalState(WellKnownContextData.ValidateCost, true);
    }
}


