using HotChocolate.Execution;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Utilities;

internal static class ErrorHelper
{
    public static IOperationResult IncrementalDelivery_NotSupported() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("Incremental delivery is not yet supported.")
                .Build());

    public static IError InvalidNodeFormat(
        ISelection selection,
        Exception? exception = null)
        => ErrorBuilder.New()
            .SetMessage("The id value has an invalid format.")
            .AddLocation(selection.SyntaxNode)
            .SetPath(new[] { selection.ResponseName, })
            .SetException(exception)
            .Build();
}
