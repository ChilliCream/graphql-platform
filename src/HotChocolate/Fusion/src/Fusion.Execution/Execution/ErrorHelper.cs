using System.Net;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Properties;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
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

    public static OperationResult StateInvalidForOperationPlanCache()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The operation plan cache requires a operation document hash.")
                .SetCode(ErrorCodes.Execution.OperationDocumentNotFound)
                .Build());

    public static OperationResult StateInvalidForVariableCoercion()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The variable coercion requires an operation execution plan.")
                .Build());

    public static IError InvalidNodeIdFormat(string originalValue)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.NodeFieldExecutionNode_InvalidNodeIdFormat)
            .SetExtension("originalValue", originalValue)
            .Build();

    public static IError AuthorizationPolicyDenied(
        Path path,
        string policyName,
        string? reason)
        => ErrorBuilder.New()
            .SetMessage(reason ?? FusionExecutionResources.ErrorHelper_AuthorizationPolicyDenied)
            .SetCode(ErrorCodes.Authentication.NotAuthorized)
            .SetPath(path)
            .SetExtension("policy", policyName)
            .Build();

    public static IError AuthorizationPolicyExecutionFailed()
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_AuthorizationPolicyExecutionFailed)
            .SetCode(ErrorCodes.Authentication.NotAuthorized)
            .Build();
}
