using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
    public static OperationResult RequestTimeout(TimeSpan timeout) =>
        OperationResult.FromError(
            new Error
            {
                Message = string.Format("The request exceeded the configured timeout of `{0}`.", timeout),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

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
}
