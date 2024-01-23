using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore;

/// <summary>
/// An internal helper class that centralizes server errors.
/// </summary>
internal static class ErrorHelper
{
    public static IError InvalidRequest()
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_InvalidRequest)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build();

    public static IError RequestHasNoElements()
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_RequestHasNoElements)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build();

    public static IError NoSupportedAcceptMediaType()
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_NoSupportedAcceptMediaType)
            .SetCode(ErrorCodes.Server.NoSupportedAcceptMediaType)
            .Build();

    public static IQueryResult TypeNameIsEmpty()
        => QueryResultBuilder.CreateError(
            new Error(
                ErrorHelper_TypeNameIsEmpty,
                code: ErrorCodes.Server.TypeParameterIsEmpty));

    public static IQueryResult InvalidTypeName(string typeName)
        => QueryResultBuilder.CreateError(
            new Error(
                ErrorHelper_InvalidTypeName,
                code: ErrorCodes.Server.InvalidTypeName,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(typeName), typeName },
                }));

    public static IQueryResult TypeNotFound(string typeName)
        => QueryResultBuilder.CreateError(
            new Error(
                string.Format(ErrorHelper_TypeNotFound, typeName),
                code: ErrorCodes.Server.TypeDoesNotExist,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(typeName), typeName },
                }));

    public static IQueryResult InvalidAcceptMediaType(string headerValue)
        => QueryResultBuilder.CreateError(
            new Error(
                string.Format(ErrorHelper_InvalidAcceptMediaType, headerValue),
                code: ErrorCodes.Server.InvalidAcceptHeaderValue,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(headerValue), headerValue },
                }));
    
    public static IQueryResult MultiPartRequestPreflightRequired()
        => QueryResultBuilder.CreateError(
            new Error(
                ErrorHelper_MultiPartRequestPreflightRequired,
                code: ErrorCodes.Server.MultiPartPreflightRequired));
}
