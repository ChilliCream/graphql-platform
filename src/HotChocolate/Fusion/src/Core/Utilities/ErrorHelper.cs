using HotChocolate.Execution;

namespace HotChocolate.Fusion.Utilities;

internal static class ErrorHelper
{
    public static IQueryResult IncrementalDelivery_NotSupported() =>
        QueryResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("Incremental delivery is not yet supported.")
                .Build());
}
