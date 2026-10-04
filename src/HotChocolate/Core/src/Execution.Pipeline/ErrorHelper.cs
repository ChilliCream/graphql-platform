using System.Collections.Immutable;
using System.Net;
using HotChocolate.Execution.Pipeline.Properties;

namespace HotChocolate.Execution.Pipeline;

internal static class ErrorHelper
{
    public static IError OperationCanceled(Exception ex)
        => new Error
        {
            Message = ExecutionPipelineResources.ErrorHelper_OperationCanceled_Message,
            Extensions = ImmutableDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Canceled),
            Exception = ex
        };

    public static NotSupportedException QueryTypeNotSupported()
        => new(ExecutionPipelineResources.ThrowHelper_QueryTypeNotSupported_Message);

    public static OperationResult OperationDocumentNotFound()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ExecutionPipelineResources.ErrorHelper_StateInvalidForDocumentValidation_Message)
                .SetCode(ErrorCodes.Execution.OperationDocumentNotFound)
                .Build());

    public static OperationResult StateInvalidForDocumentValidation()
    {
        var result = OperationDocumentNotFound();

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.InternalServerError);

        return result;
    }
}
