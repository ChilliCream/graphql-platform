using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal static class ErrorHelper
    {
        public static IError ArgumentNonNullError(
            ArgumentNode argument,
            string responseName,
            ArgumentNonNullValidator.ValidationResult validationResult)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "Detected a non-null violation in argument `{0}`.",
                    argument.Name.Value)
                .AddLocation(argument)
                .SetExtension("responseName", responseName)
                .SetExtension("errorPath", validationResult.Path)
                .Build();
        }

        public static IError ArgumentValueIsInvalid(
            ArgumentNode argument,
            string responseName,
            ScalarSerializationException exception)
        {
            return ErrorBuilder.New()
                .SetMessage(exception.Message)
                .AddLocation(argument)
                .SetExtension("responseName", responseName)
                .Build();
        }

        public static IError ArgumentDefaultValueIsInvalid(
            string responseName,
            ScalarSerializationException exception)
        {
            return ErrorBuilder.New()
                .SetMessage(exception.Message)
                .SetExtension("responseName", responseName)
                .Build();
        }

        public static IError InvalidLeafValue(
            ScalarSerializationException exception,
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
            IErrorHandler errorHandler,
            FieldNode field,
            Path path)
        {
            return errorHandler
                .CreateUnexpectedError(exception)
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
                .SetMessage("Unable to resolve the abstract type `{0}`.", typeName)
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
                .SetMessage("Unable to resolve the abstract type `{0}`.", typeName)
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
                .SetMessage("The type `{0}` is not supported as list value.", listType.FullName!)
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
                .SetMessage("Unexpected error during value completion.")
                .AddLocation(field)
                .SetPath(path)
                .SetCode(ErrorCodes.Execution.ListTypeNotSupported)
                .Build();
        }

        public static IReadOnlyQueryResult ParserExpectedQuery() =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage("The parse query middleware expects a valid query request.")
                    .SetCode(ErrorCodes.Execution.Incomplete)
                    .Build());

        public static IReadOnlyQueryResult RootTypeNotFound(OperationType operationType) =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage(
                        "The specified root type `{0}` is not supported by this server.",
                        operationType)
                    .Build());

        public static IReadOnlyQueryResult StateInvalidForOperationResolver() =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage(
                        "Either no query document exists or the document " +
                        "validation result is invalid.")
                    .Build());

        public static IReadOnlyQueryResult StateInvalidForOperationVariableCoercion() =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage(
                        "There is no operation on the context which can be used to coerce " +
                        "variables.")
                    .Build());

        public static IReadOnlyQueryResult StateInvalidForOperationExecution() =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage(
                        "Either now compiled operation was found or the variables " +
                        "have not been coerced.")
                    .Build());

        public static IError ValueCompletion_CouldNotResolveAbstractType(
            FieldNode field,
            Path path) =>
             ErrorBuilder.New()
                    .SetMessage("Could not resolve the actual object type from `System.String` for the abstract type `Bar`.")
                    .SetPath(path)
                    .AddLocation(field)
                    .Build();
    }
}
