using System.Collections.Immutable;
using System.Net;
using HotChocolate.Collections.Immutable;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.CostAnalysis;
using HotChocolate.Fusion.Properties;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
    private static readonly ImmutableDictionary<string, object?> s_validationError
        = ImmutableDictionary<string, object?>.Empty.Add(
            ExecutionContextData.ValidationErrors,
            true);

    public static OperationResult IncrementalDeliveryNotAcceptable()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_IncrementalDeliveryNotAcceptable)
                .Build());

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.NotAcceptable);

        return result;
    }

    public static OperationResult OperationKindNotAllowed(RequestFlags requiredFlag)
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_OperationKindNotAllowed)
                .Build());

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.OperationNotAllowed,
            requiredFlag);

        return result;
    }

    public static OperationResult RequestTimeout(TimeSpan timeout)
    {
        var result = OperationResult.FromError(
            new Error
            {
                Message = string.Format("The request exceeded the configured timeout of `{0}`.", timeout),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.InternalServerError);

        return result;
    }

    public static OperationResult StateInvalidForCostAnalysis()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The cost analysis requires a normalized operation document.")
                .SetCode(ErrorCodes.Execution.CostStateInvalid)
                .Build());

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.InternalServerError);

        return result;
    }

    public static OperationResult StateInvalidForCostAnalysisMissingVariableValues()
        => RequestError(
            ErrorBuilder.New()
                .SetMessage("The cost analysis requires at least one coerced variable value set.")
                .SetCode(ErrorCodes.Execution.CostStateInvalid)
                .Build());

    public static OperationResult ResponseSizeAnalysisNotEnabled()
        => RequestError(
            ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_ResponseSizeAnalysisNotEnabled)
                .SetCode(ErrorCodes.Execution.ResponseSizeAnalysisNotEnabled)
                .Build());

    public static OperationResult MaxFieldCostReached(
        CostEstimate estimate,
        double maxFieldCost)
        => CostExceeded(
            FusionExecutionResources.ErrorHelper_MaxFieldCostReached,
            ImmutableOrderedDictionary<string, object?>.Empty
                .Add("code", ErrorCodes.Execution.CostExceeded)
                .Add("fieldCost", CostResultHelper.FormatValue(estimate.FieldCost))
                .Add("maxFieldCost", CostResultHelper.FormatValue(maxFieldCost)));

    public static OperationResult MaxTypeCostReached(
        CostEstimate estimate,
        double maxTypeCost)
        => CostExceeded(
            FusionExecutionResources.ErrorHelper_MaxTypeCostReached,
            ImmutableOrderedDictionary<string, object?>.Empty
                .Add("code", ErrorCodes.Execution.CostExceeded)
                .Add("typeCost", CostResultHelper.FormatValue(estimate.TypeCost))
                .Add("maxTypeCost", CostResultHelper.FormatValue(maxTypeCost)));

    public static OperationResult MaxResponseSizeReached(
        CostEstimate estimate,
        double maxAllowedResponseSize)
        => CostExceeded(
            FusionExecutionResources.ErrorHelper_MaxResponseSizeReached,
            ImmutableOrderedDictionary<string, object?>.Empty
                .Add("code", ErrorCodes.Execution.CostExceeded)
                .Add(
                    "maxResponseSize",
                    CostResultHelper.FormatValue(estimate.MaxResponseSize.GetValueOrDefault()))
                .Add(
                    "maxAllowedResponseSize",
                    CostResultHelper.FormatValue(maxAllowedResponseSize)));

    private static OperationResult CostExceeded(
        string message,
        ImmutableOrderedDictionary<string, object?> extensions)
        => RequestError(
            new Error
            {
                Message = message,
                Extensions = extensions
            });

    private static OperationResult RequestError(IError error)
    {
        var result = OperationResult.FromError(error);
        result.ContextData = s_validationError;
        return result;
    }

    public static IError InvalidNodeIdFormat(string originalValue)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.NodeFieldExecutionNode_InvalidNodeIdFormat)
            .SetExtension("originalValue", originalValue)
            .Build();
}
