using System.Collections.Immutable;
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
            new Error
            {
                Message = ErrorHelper_TypeNameIsEmpty,
                Extensions = ImmutableDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.TypeParameterIsEmpty)
            });

    public static IOperationResult InvalidTypeName(string typeName)
        => OperationResultBuilder.CreateError(
            new Error
            {
                Message = ErrorHelper_InvalidTypeName,
                Extensions = ImmutableDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.InvalidTypeName)
                    .Add(nameof(typeName), typeName)
            });

    public static IOperationResult TypeNotFound(string typeName)
        => OperationResultBuilder.CreateError(
            new Error
            {
                Message = string.Format(ErrorHelper_TypeNotFound, typeName),
                Extensions = ImmutableDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.TypeDoesNotExist)
                    .Add(nameof(typeName), typeName)
            });

    public static IOperationResult InvalidAcceptMediaType(string headerValue)
        => OperationResultBuilder.CreateError(
            new Error
            {
                Message = string.Format(ErrorHelper_InvalidAcceptMediaType, headerValue),
                Extensions = ImmutableDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.InvalidAcceptHeaderValue)
                    .Add(nameof(headerValue), headerValue)
            });

    public static IOperationResult MultiPartRequestPreflightRequired()
        => OperationResultBuilder.CreateError(
            new Error
            {
                Message = ErrorHelper_MultiPartRequestPreflightRequired,
                Extensions = ImmutableDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.MultiPartPreflightRequired)
            });

    public static GraphQLRequestException InvalidQueryIdFormat()
        => new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage("Invalid query id format.")
                .Build());

    public static IExecutionResult OperationNameRequired()
        => OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("The operation name is required.")
                .Build());
}
