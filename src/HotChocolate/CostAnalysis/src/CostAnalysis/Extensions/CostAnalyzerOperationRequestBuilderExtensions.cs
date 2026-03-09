namespace HotChocolate.Execution;

/// <summary>
/// Cost Analyzer extensions for the <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class CostAnalyzerOperationRequestBuilderExtensions
{
    public static OperationRequestBuilder ReportCost(this OperationRequestBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RemoveGlobalState(ExecutionContextData.ValidateCost);
        return builder.AddGlobalState(ExecutionContextData.ReportCost, true);
    }

    public static OperationRequestBuilder ValidateCost(this OperationRequestBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RemoveGlobalState(ExecutionContextData.ReportCost);
        return builder.AddGlobalState(ExecutionContextData.ValidateCost, true);
    }
}
