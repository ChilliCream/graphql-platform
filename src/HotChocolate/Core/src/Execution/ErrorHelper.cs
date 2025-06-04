using System.Collections.Immutable;
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
            .AddLocation(argument)
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
            .AddLocation(argument)
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
            .AddLocation(field)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnexpectedLeafValueSerializationError(
        Exception exception,
        FieldNode field,
        Path path)
    {
        return ErrorBuilder
            .FromException(exception)
            .AddLocation(field)
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
            .AddLocation(field)
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
            .AddLocation(field)
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
            .AddLocation(field)
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
            .AddLocation(field)
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
                { WellKnownContextData.HttpStatusCode, HttpStatusCode.BadRequest }
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
            .AddLocation(field)
            .Build();

    public static IOperationResult OperationKindNotAllowed() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("The specified operation kind is not allowed.")
                .Build(),
            new Dictionary<string, object?>
            {
                { WellKnownContextData.OperationNotAllowed, null }
            });

    public static IOperationResult RequestTypeNotAllowed() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("Variable batch requests are only allowed for mutations and subscriptions.")
                .Build(),
            new Dictionary<string, object?>
            {
                { ExecutionContextData.ValidationErrors, null }
            });

    public static IOperationResult RequestTimeout(TimeSpan timeout) =>
        OperationResultBuilder.CreateError(
            new Error
            {
                Message = string.Format(ErrorHelper_RequestTimeout, timeout),
                Extensions = ImmutableDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

    public static IError NonNullOutputFieldViolation(Path? path, FieldNode selection)
        => ErrorBuilder.New()
            .SetMessage("Cannot return null for non-nullable field.")
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetPath(path)
            .AddLocation(selection)
            .Build();
}
