using HotChocolate.CostAnalysis.Properties;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Execution;

namespace HotChocolate.CostAnalysis;

internal static class ErrorHelper
{
    public static IExecutionResult MaxFieldCostReached(
        CostMetrics costMetrics,
        double maxFieldCost,
        bool reportMetrics)
        => ResultHelper.CreateError(
            new Error(
                CostAnalysisResources.ErrorHelper_MaxFieldCostReached,
                ErrorCodes.Execution.ComplexityExceeded,
                extensions: new Dictionary<string, object?>
                {
                    { "fieldCost", costMetrics.FieldCost },
                    { nameof(maxFieldCost), maxFieldCost }
                }),
            reportMetrics ? costMetrics : null);

    public static IExecutionResult MaxTypeCostReached(
        CostMetrics costMetrics,
        double maxTypeCost,
        bool reportMetrics)
        => ResultHelper.CreateError(
            new Error(
                CostAnalysisResources.ErrorHelper_MaxTypeCostReached,
                ErrorCodes.Execution.ComplexityExceeded,
                extensions: new Dictionary<string, object?>
                {
                    { "typeCost", costMetrics.TypeCost },
                    { nameof(maxTypeCost), maxTypeCost }
                }),
            reportMetrics ? costMetrics : null);
}
