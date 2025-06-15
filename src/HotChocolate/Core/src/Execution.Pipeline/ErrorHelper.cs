using System.Collections.Immutable;
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

    public static IOperationResult StateInvalidForDocumentValidation()
        => OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ExecutionPipelineResources.ErrorHelper_StateInvalidForDocumentValidation_Message)
                .SetCode(ErrorCodes.Execution.OperationDocumentNotFound)
                .Build());
}
