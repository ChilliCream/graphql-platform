using HotChocolate.Execution;
using HotChocolate.Server.Properties;

namespace HotChocolate.Server;

/// <summary>
/// An internal helper class that centralizes server errors.
/// </summary>
internal static class ErrorHelper
{
    public static IError InvalidRequest() =>
        ErrorBuilder.New()
            .SetMessage(ServerResources.ErrorHelper_InvalidRequest)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build();

    public static IError RequestHasNoElements() =>
        ErrorBuilder.New()
            .SetMessage(ServerResources.ErrorHelper_RequestHasNoElements)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build();

    public static IQueryResult ResponseTypeNotSupported() =>
        QueryResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ServerResources.ErrorHelper_ResponseTypeNotSupported)
                .Build());

    public static IQueryResult UnknownSubscriptionError(Exception ex)
    {
        IError error =
            ErrorBuilder
                .New()
                .SetException(ex)
                .SetCode(ErrorCodes.Execution.TaskProcessingError)
                .SetMessage(ServerResources.Subscription_SendResultsAsync)
                .Build();

        return QueryResultBuilder.CreateError(error);
    }
}
