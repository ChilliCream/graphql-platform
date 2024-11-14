using System.Net;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution;

internal static class ErrorHelper
{
    public static IError ArgumentNonNullError(
        ArgumentNode argument,
        string responseName,
        ArgumentNonNullValidator.ValidationResult validationResult)
    {
        return ErrorBuilder.New()
            .SetMessage(
                ErrorHelper_ArgumentNonNullError_Message,
                argument.Name.Value)
            .SetLocations([argument])
            .SetExtension("responseName", responseName)
            .SetExtension("errorPath", validationResult.Path)
            .Build();
    }

    public static IError ArgumentValueIsInvalid(
        ArgumentNode argument,
        string responseName,
        GraphQLException exception)
    {
        return ErrorBuilder.FromError(exception.Errors[0])
            .SetLocations([argument])
            .SetExtension("responseName", responseName)
            .Build();
    }

    public static IError ArgumentDefaultValueIsInvalid(
        string responseName,
        GraphQLException exception)
    {
        return ErrorBuilder.FromError(exception.Errors[0])
            .SetExtension("responseName", responseName)
            .Build();
    }

    public static IError InvalidLeafValue(
        GraphQLException exception,
        FieldNode field,
        Path path)
    {
        return ErrorBuilder.FromError(exception.Errors[0])
            .SetLocations([field])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnexpectedLeafValueSerializationError(
        Exception exception,
        IErrorHandler errorHandler,
        FieldNode field,
        Path path)
    {
        return errorHandler
            .CreateUnexpectedError(exception)
            .SetLocations([field])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnableToResolveTheAbstractType(
        string typeName,
        FieldNode field,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnableToResolveTheAbstractType_Message, typeName)
            .SetLocations([field])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotResolveAbstractType)
            .Build();
    }

    public static IError UnexpectedErrorWhileResolvingAbstractType(
        Exception exception,
        string typeName,
        FieldNode field,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnableToResolveTheAbstractType_Message, typeName)
            .SetLocations([field])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotResolveAbstractType)
            .SetException(exception)
            .Build();
    }

    public static IError ListValueIsNotSupported(
        Type listType,
        FieldNode field,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_ListValueIsNotSupported_Message, listType.FullName!)
            .SetLocations([field])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.ListTypeNotSupported)
            .Build();
    }

    public static IError UnexpectedValueCompletionError(
        FieldNode field,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnexpectedValueCompletionError_Message)
            .SetLocations([field])
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.ListTypeNotSupported)
            .Build();
    }

    public static IOperationResult RootTypeNotFound(OperationType operationType) =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_RootTypeNotFound_Message, operationType)
                .Build(),
            new Dictionary<string, object?>
            {
                { WellKnownContextData.HttpStatusCode, HttpStatusCode.BadRequest },
            });

    public static IOperationResult StateInvalidForOperationResolver() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationResolver_Message)
                .Build());

    public static IOperationResult StateInvalidForOperationVariableCoercion() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationVariableCoercion_Message)
                .Build());

    public static IOperationResult StateInvalidForOperationExecution() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationExecution_Message)
                .Build());

    public static IError ValueCompletion_CouldNotResolveAbstractType(
        FieldNode field,
        Path path,
        object result) =>
        ErrorBuilder.New()
            .SetMessage(
                ErrorHelper_ValueCompletion_CouldNotResolveAbstractType_Message,
                result.GetType().FullName ?? result.GetType().Name,
                field.Name)
            .SetPath(path)
            .SetLocations([field])
            .Build();

    public static IOperationResult StateInvalidForDocumentValidation() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForDocumentValidation_Message)
                .SetCode(ErrorCodes.Execution.QueryNotFound)
                .Build());

    public static IOperationResult OperationKindNotAllowed() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("The specified operation kind is not allowed.")
                .Build(),
            new Dictionary<string, object?>
            {
                { WellKnownContextData.OperationNotAllowed, null },
            });

    public static IOperationResult RequestTypeNotAllowed() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("Variable batch requests are only allowed for mutations and subscriptions.")
                .Build(),
            new Dictionary<string, object?>
            {
                { WellKnownContextData.ValidationErrors, null },
            });

    public static IOperationResult RequestTimeout(TimeSpan timeout) =>
        OperationResultBuilder.CreateError(
            new Error(
                string.Format(ErrorHelper_RequestTimeout, timeout),
                ErrorCodes.Execution.Timeout));

    public static IOperationResult OperationCanceled() =>
        OperationResultBuilder.CreateError(
            new Error(
                ErrorHelper_OperationCanceled_Message,
                ErrorCodes.Execution.Canceled));

    public static IError NonNullOutputFieldViolation(Path? path, FieldNode selection)
        => ErrorBuilder.New()
            .SetMessage("Cannot return null for non-nullable field.")
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetPath(path)
            .SetLocations([selection])
            .Build();

    public static IError PersistedOperationNotFound(OperationDocumentId requestedKey)
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_PersistedOperationNotFound)
            .SetCode(ErrorCodes.Execution.PersistedOperationNotFound)
            .SetExtension(nameof(requestedKey), requestedKey)
            .Build();

    public static IError OnlyPersistedOperationsAreAllowed()
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_OnlyPersistedOperationsAreAllowed)
            .SetCode(ErrorCodes.Execution.OnlyPersistedOperationsAllowed)
            .Build();

    public static IError ReadPersistedOperationMiddleware_PersistedOperationNotFound()
        => ErrorBuilder.New()
            // this string is defined in the APQ spec!
            .SetMessage("PersistedQueryNotFound")
            .SetCode(ErrorCodes.Execution.PersistedOperationNotFound)
            .Build();
}
