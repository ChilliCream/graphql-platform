using System.Collections.Immutable;
using System.Net;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using static HotChocolate.Properties.Resources;

namespace HotChocolate.Execution;

internal static class ErrorHelper
{
    public static IError ArgumentNonNullError(
        ArgumentNode argument,
        ArgumentNonNullValidator.ValidationResult validationResult)
    {
        return ErrorBuilder.New()
            .SetMessage(
                ErrorHelper_ArgumentNonNullError_Message,
                argument.Name.Value)
            .SetInputPath(validationResult.Path)
            .Build();
    }

    public static IError InvalidLeafValue(
        GraphQLException exception,
        Path path)
    {
        return ErrorBuilder.FromError(exception.Errors[0])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnexpectedLeafValueSerializationError(
        Exception exception,
        Path path)
    {
        return ErrorBuilder
            .FromException(exception)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnableToResolveTheAbstractType(
        string typeName,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnableToResolveTheAbstractType_Message, typeName)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotResolveAbstractType)
            .Build();
    }

    public static IError UnexpectedErrorWhileResolvingAbstractType(
        Exception exception,
        string typeName,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnableToResolveTheAbstractType_Message, typeName)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotResolveAbstractType)
            .SetException(exception)
            .Build();
    }

    public static IError ListValueIsNotSupported(
        Type listType,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_ListValueIsNotSupported_Message, listType.FullName)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.ListTypeNotSupported)
            .Build();
    }

    public static IError UnexpectedValueCompletionError(
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnexpectedValueCompletionError_Message)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.ListTypeNotSupported)
            .Build();
    }

    public static OperationResult RootTypeNotFound(OperationType operationType)
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_RootTypeNotFound_Message, operationType)
                .Build());
        result.ContextData = result.ContextData.Add(ExecutionContextData.HttpStatusCode, HttpStatusCode.BadRequest);
        return result;
    }

    public static OperationResult StateInvalidForOperationResolver()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationResolver_Message)
                .Build());

    public static OperationResult StateInvalidForOperationExecution()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationExecution_Message)
                .Build());

    public static IError ValueCompletion_CouldNotResolveAbstractType(
        Selection selection,
        Path path,
        object result)
        => ErrorBuilder.New()
            .SetMessage(
                ErrorHelper_ValueCompletion_CouldNotResolveAbstractType_Message,
                result.GetType().FullName ?? result.GetType().Name,
                selection.ResponseName)
            .SetPath(path)
            .Build();

    public static OperationResult OperationKindNotAllowed(RequestFlags requiredFlag)
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The specified operation kind is not allowed.")
                .Build());

        // The flag the operation kind required lets the transport tell a refusal the request
        // method can resolve from one it cannot, which is the difference between 405 and 406.
        result.ContextData = result.ContextData.Add(
            ExecutionContextData.OperationNotAllowed,
            requiredFlag);

        return result;
    }

    public static OperationResult IncrementalDeliveryNotAcceptable()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_IncrementalDeliveryNotAcceptable)
                .Build());

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.NotAcceptable);

        return result;
    }

    public static OperationResult RequestTypeNotAllowed()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("Variable batch requests are only allowed for queries and mutations.")
                .Build());

        result.ContextData = result.ContextData.Add(ExecutionContextData.ValidationErrors, null);

        return result;
    }

    public static OperationResult RequestTimeout(TimeSpan timeout)
    {
        var result = OperationResult.FromError(
            new Error
            {
                Message = string.Format(ErrorHelper_RequestTimeout, timeout),
                Extensions = ImmutableDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.InternalServerError);

        return result;
    }

    public static ErrorBuilder NonNullOutputFieldViolation()
        => ErrorBuilder.New()
            .SetMessage("Cannot return null for non-nullable field.")
            .SetCode(ErrorCodes.Execution.NonNullViolation);
}
