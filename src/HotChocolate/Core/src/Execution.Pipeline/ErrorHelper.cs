using System.Collections.Immutable;

namespace HotChocolate.Execution.Pipeline;

internal static class ErrorHelper
{
    public static IError OperationCanceled(Exception ex)
        => new Error
            {
                Message = ErrorHelper_OperationCanceled_Message,
                Extensions = ImmutableDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Canceled),
                Exception = ex
            };
}