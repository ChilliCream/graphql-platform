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
            .AddLocation(argument)
            .SetInputPath(validationResult.Path)
            .Build();
    }

    public static IError InvalidLeafValue(
        GraphQLException exception,
        Selection selection,
        Path path)
    {
        return ErrorBuilder.FromError(exception.Errors[0])
            .AddLocations(selection)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnexpectedLeafValueSerializationError(
        Exception exception,
        Selection selection,
        Path path)
    {
        return ErrorBuilder
            .FromException(exception)
            .AddLocations(selection)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotSerializeLeafValue)
            .Build();
    }

    public static IError UnableToResolveTheAbstractType(
        string typeName,
        Selection selection,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnableToResolveTheAbstractType_Message, typeName)
            .AddLocations(selection)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotResolveAbstractType)
            .Build();
    }

    public static IError UnexpectedErrorWhileResolvingAbstractType(
        Exception exception,
        string typeName,
        Selection selection,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnableToResolveTheAbstractType_Message, typeName)
            .AddLocations(selection)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.CannotResolveAbstractType)
            .SetException(exception)
            .Build();
    }

    public static IError ListValueIsNotSupported(
        Type listType,
        Selection selection,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_ListValueIsNotSupported_Message, listType.FullName)
            .AddLocations(selection)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.ListTypeNotSupported)
            .Build();
    }

    public static IError UnexpectedValueCompletionError(
        Selection selection,
        Path path)
    {
        return ErrorBuilder.New()
            .SetMessage(ErrorHelper_UnexpectedValueCompletionError_Message)
            .AddLocations(selection)
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

    public static OperationResult StateInvalidForOperationVariableCoercion()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage(ErrorHelper_StateInvalidForOperationVariableCoercion_Message)
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
            .AddLocations(selection)
            .Build();

    public static OperationResult OperationKindNotAllowed()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The specified operation kind is not allowed.")
                .Build());

        result.ContextData = result.ContextData.Add(ExecutionContextData.OperationNotAllowed, null);

        return result;
    }

    public static OperationResult RequestTypeNotAllowed()
    {
        var result = OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("Variable batch requests are only allowed for mutations and subscriptions.")
                .Build());

        result.ContextData = result.ContextData.Add(ExecutionContextData.ValidationErrors, null);

        return result;
    }

    public static OperationResult RequestTimeout(TimeSpan timeout)
        => OperationResult.FromError(
            new Error
            {
                Message = string.Format(ErrorHelper_RequestTimeout, timeout),
                Extensions = ImmutableDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

    public static ErrorBuilder NonNullOutputFieldViolation()
        => ErrorBuilder.New()
            .SetMessage("Cannot return null for non-nullable field.")
            .SetCode(ErrorCodes.Execution.NonNullViolation);
}
