using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
    public static OperationResult RequestTimeout(TimeSpan timeout) =>
        OperationResult.FromError(
            new Error
            {
                Message = string.Format(ErrorHelper_RequestTimeout_Message, timeout),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

    public static OperationResult StateInvalidForOperationPlanCache()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationPlanCache_Message)
                .SetCode(ErrorCodes.Execution.OperationDocumentNotFound)
                .Build());

    public static OperationResult StateInvalidForVariableCoercion()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForVariableCoercion_Message)
                .Build());

    public static IError UnexpectedExecutionError()
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnexpectedExecutionError_Message)
            .Build();

    public static IError NonNullOutputFieldViolation(Path path)
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_NonNullOutputFieldViolation_Message)
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetPath(path)
            .Build();
}
