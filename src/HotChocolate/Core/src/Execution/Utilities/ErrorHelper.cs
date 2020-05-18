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
            IErrorHandler errorHandler,
            FieldNode field,
            Path path)
        {
            return errorHandler
                .CreateUnexpectedError(exception)
                .SetMessage(exception.Message)
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
    }
}
