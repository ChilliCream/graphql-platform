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

    public static IOperationResult TypeNameIsEmpty()
        => OperationResultBuilder.CreateError(
            new Error(
                ErrorHelper_TypeNameIsEmpty,
                code: ErrorCodes.Server.TypeParameterIsEmpty));

    public static IOperationResult InvalidTypeName(string typeName)
        => OperationResultBuilder.CreateError(
            new Error(
                ErrorHelper_InvalidTypeName,
                code: ErrorCodes.Server.InvalidTypeName,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(typeName), typeName },
                }));

    public static IOperationResult TypeNotFound(string typeName)
        => OperationResultBuilder.CreateError(
            new Error(
                string.Format(ErrorHelper_TypeNotFound, typeName),
                code: ErrorCodes.Server.TypeDoesNotExist,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(typeName), typeName },
                }));

    public static IOperationResult InvalidAcceptMediaType(string headerValue)
        => OperationResultBuilder.CreateError(
            new Error(
                string.Format(ErrorHelper_InvalidAcceptMediaType, headerValue),
                code: ErrorCodes.Server.InvalidAcceptHeaderValue,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(headerValue), headerValue },
                }));
    
    public static IOperationResult MultiPartRequestPreflightRequired()
        => OperationResultBuilder.CreateError(
            new Error(
                ErrorHelper_MultiPartRequestPreflightRequired,
                code: ErrorCodes.Server.MultiPartPreflightRequired));
    
    public static GraphQLRequestException InvalidQueryIdFormat()
        => new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage("Invalid query id format.")
                .Build());
}
