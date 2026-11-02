using System.Collections.Immutable;
using System.Net;
using HotChocolate.Collections.Immutable;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.CostAnalysis;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Properties;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
    private static readonly ImmutableDictionary<string, object?> s_validationError
        = ImmutableDictionary<string, object?>.Empty.Add(
            ExecutionContextData.ValidationErrors,
            true);

    public static OperationResult EmptyVariableBatch()
        => RequestError(
            ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_EmptyVariableBatch)
                .Build());

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

    public static IError DeniedField(
        AuthorizationDenialKind kind,
        Path path,
        PolicyDescriptor? attribution)
    {
        var builder = CreateDenialBuilder(kind).SetPath(path);

        if (attribution is not null)
        {
            AddAttribution(builder, attribution);
        }

        return builder.Build();
    }

    public static IError DeniedNonNullField(Path path)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_NonNullViolation)
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetPath(path)
            .Build();

    public static OperationResult AuthorizationRejected(
        SelectionDenial denial,
        bool isAttributed,
        string? challenge)
    {
        var builder = CreateDenialBuilder(denial.Kind);

        if (isAttributed)
        {
            builder.SetExtension("coordinate", denial.Selection.Field.Coordinate.ToString());
            AddAttribution(builder, denial.Descriptor);
        }

        var result = OperationResult.FromError(builder.Build());

        if (denial.Kind is AuthorizationDenialKind.Unauthenticated)
        {
            result.ContextData = result.ContextData.Add(
                ExecutionContextData.HttpStatusCode,
                HttpStatusCode.Unauthorized);

            if (challenge is not null)
            {
                result.ContextData = result.ContextData.Add(
                    ExecutionContextData.WwwAuthenticateHeaderValue,
                    challenge);
            }
        }
        else
        {
            result.ContextData = result.ContextData.Add(
                ExecutionContextData.HttpStatusCode,
                HttpStatusCode.Forbidden);
        }

        return result;
    }

    public static OperationResult AuthorizationFailed()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_AuthorizationFailed)
                .Build());

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.InternalServerError);

        return result;
    }

    private static ErrorBuilder CreateDenialBuilder(AuthorizationDenialKind kind)
        => kind is AuthorizationDenialKind.Unauthenticated
            ? ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_NotAuthenticated)
                .SetCode(ErrorCodes.Authentication.NotAuthenticated)
            : ErrorBuilder.New()
                .SetMessage(FusionExecutionResources.ErrorHelper_NotAuthorized)
                .SetCode(ErrorCodes.Authentication.NotAuthorized);

    private static void AddAttribution(ErrorBuilder builder, PolicyDescriptor descriptor)
    {
        builder.SetExtension("directive", descriptor.DirectiveName);

        if (descriptor.PolicyName is not null)
        {
            builder.SetExtension("policy", descriptor.PolicyName);
        }

        if (!descriptor.Scopes.IsEmpty)
        {
            builder.SetExtension("requiredScopes", descriptor.Scopes);
        }
    }

    public static IError InvalidNodeIdFormat(string originalValue)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.NodeFieldExecutionNode_InvalidNodeIdFormat)
            .SetExtension("originalValue", originalValue)
            .Build();
}
