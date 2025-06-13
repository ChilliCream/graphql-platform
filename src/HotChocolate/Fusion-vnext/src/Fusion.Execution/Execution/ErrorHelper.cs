using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
    public static IOperationResult RequestTimeout(TimeSpan timeout) =>
        OperationResultBuilder.CreateError(
            new Error
            {
                Message = string.Format("The request exceeded the configured timeout of `{0}`.", timeout),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });
}