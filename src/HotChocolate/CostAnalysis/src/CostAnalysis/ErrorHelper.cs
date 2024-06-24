using HotChocolate.CostAnalysis.Properties;
using HotChocolate.Execution;

namespace HotChocolate.CostAnalysis;

internal static class ErrorHelper
{
    public static IOperationResult MaxFieldCostReached(double fieldCost, double maxFieldCost)
    {
        return OperationResultBuilder.CreateError(
            new Error(
                CostAnalysisResources.ErrorHelper_MaxFieldCostReached,
                ErrorCodes.Execution.ComplexityExceeded, // FIXME: Add error code.
                extensions: new Dictionary<string, object?>
                {
                    { nameof(fieldCost), fieldCost },
                    { nameof(maxFieldCost), maxFieldCost }
                }),
            contextData: new Dictionary<string, object?>
            {
                { WellKnownContextData.ValidationErrors, true } // FIXME: Should this remain?
            });
    }

    public static IOperationResult MaxTypeCostReached(double typeCost, double maxTypeCost)
    {
        return OperationResultBuilder.CreateError(
            new Error(
                CostAnalysisResources.ErrorHelper_MaxTypeCostReached,
                ErrorCodes.Execution.ComplexityExceeded, // FIXME: Add error code.
                extensions: new Dictionary<string, object?>
                {
                    { nameof(typeCost), typeCost },
                    { nameof(maxTypeCost), maxTypeCost }
                }),
            contextData: new Dictionary<string, object?>
            {
                { WellKnownContextData.ValidationErrors, true } // FIXME: Should this remain?
            });
    }
}
