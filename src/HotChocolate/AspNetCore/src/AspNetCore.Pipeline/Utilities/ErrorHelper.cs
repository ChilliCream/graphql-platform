using HotChocolate.Collections.Immutable;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;

namespace HotChocolate.AspNetCore.Utilities;

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

    public static GraphQLRequestException InvalidRequest(
        InvalidGraphQLRequestException ex) =>
        new(ErrorBuilder.New()
            .SetMessage(ex.Message)
            .SetCode(ErrorCodes.Server.RequestInvalid)
            .Build());

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

    public static OperationResult TypeNameIsEmpty()
        => OperationResult.FromError(
            new Error
            {
                Message = ErrorHelper_TypeNameIsEmpty,
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.TypeParameterIsEmpty)
            });

    public static OperationResult InvalidTypeName(string typeName)
        => OperationResult.FromError(
            new Error
            {
                Message = ErrorHelper_InvalidTypeName,
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.InvalidTypeName)
                    .Add(nameof(typeName), typeName)
            });

    public static OperationResult TypeNotFound(string typeName)
        => OperationResult.FromError(
            new Error
            {
                Message = string.Format(ErrorHelper_TypeNotFound, typeName),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.TypeDoesNotExist)
                    .Add(nameof(typeName), typeName)
            });

    public static OperationResult InvalidAcceptMediaType(string headerValue)
        => OperationResult.FromError(
            new Error
            {
                Message = string.Format(ErrorHelper_InvalidAcceptMediaType, headerValue),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.InvalidAcceptHeaderValue)
                    .Add(nameof(headerValue), headerValue)
            });

    public static OperationResult MultiPartRequestPreflightRequired()
        => OperationResult.FromError(
            new Error
            {
                Message = ErrorHelper_MultiPartRequestPreflightRequired,
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty
                    .Add("code", ErrorCodes.Server.MultiPartPreflightRequired)
            });

    public static GraphQLRequestException InvalidOperationIdFormat()
        => new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage("The operation id has an invalid format.")
                .Build());

    public static IExecutionResult OperationNameRequired()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The operation name is required.")
                .Build());
}
