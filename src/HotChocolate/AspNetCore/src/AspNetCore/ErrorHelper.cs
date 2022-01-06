using HotChocolate.AspNetCore.Properties;

namespace HotChocolate.AspNetCore;

/// <summary>
/// An internal helper class that centralizes server errors.
/// </summary>
internal static class ErrorHelper
{
    public static IError InvalidRequest()
        => ErrorBuilder.New()
            .SetMessage(AspNetCoreResources.ErrorHelper_InvalidRequest)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build();

    public static IError RequestHasNoElements()
        => ErrorBuilder.New()
            .SetMessage(AspNetCoreResources.ErrorHelper_RequestHasNoElements)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build();

    public static IQueryResult ResponseTypeNotSupported()
        => QueryResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(AspNetCoreResources.ErrorHelper_ResponseTypeNotSupported)
                .Build());

    public static IQueryResult UnknownSubscriptionError(Exception ex)
        => QueryResultBuilder.CreateError(
            ErrorBuilder
                .New()
                .SetException(ex)
                .SetCode(ErrorCodes.Execution.TaskProcessingError)
                .SetMessage(AspNetCoreResources.Subscription_SendResultsAsync)
                .Build());

    public static IQueryResult TypeNameIsEmpty()
        => QueryResultBuilder.CreateError(
            new Error(
                "The specified types argument is empty.",
                code: ErrorCodes.Server.TypeParameterIsEmpty));

    public static IQueryResult InvalidTypeName(string typeName)
        => QueryResultBuilder.CreateError(
            new Error(
                "The type name is invalid.",
                code: ErrorCodes.Server.InvalidTypeName,
                extensions: new Dictionary<string, object?>
                {
                    { "typeName", typeName }
                }));

    public static IQueryResult TypeNotFound(string typeName)
        => QueryResultBuilder.CreateError(
            new Error(
                $"The type `{typeName}` does not exist.",
                code: ErrorCodes.Server.TypeDoesNotExist,
                extensions: new Dictionary<string, object?>
                {
                    { "typeName", typeName }
                }));
}
