using System;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal static class ThrowHelper
    {
        public static GraphQLException VariableIsNotAnInputType(
            VariableDefinitionNode variableDefinition)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        "Variable `{0}` is not an input type.",
                        variableDefinition.Variable.Name.Value)
                    .SetCode(ErrorCodes.Execution.NonNullViolation)
                    .SetExtension("variable", variableDefinition.Variable.Name.Value)
                    .SetExtension("type", variableDefinition.Type.ToString()!)
                    .AddLocation(variableDefinition)
                    .Build());
        }

        public static GraphQLException NonNullVariableIsNull(
            VariableDefinitionNode variableDefinition)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        "Variable `{0}` is required.",
                        variableDefinition.Variable.Name.Value)
                    .SetCode(ErrorCodes.Execution.NonNullViolation)
                    .SetExtension("variable", variableDefinition.Variable.Name.Value)
                    .AddLocation(variableDefinition)
                    .Build());
        }

        public static GraphQLException VariableValueInvalidType(
            VariableDefinitionNode variableDefinition,
            Exception? exception = null)
        {
            IErrorBuilder errorBuilder = ErrorBuilder.New()
                .SetMessage(
                    "Variable `{0}` got an invalid value.",
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.InvalidType)
                .SetExtension("variable", variableDefinition.Variable.Name.Value)
                .AddLocation(variableDefinition);

            switch (exception)
            {
                case ScalarSerializationException ex:
                    errorBuilder.SetExtension("scalarError", ex.Message);
                    break;
                case InputObjectSerializationException ex:
                    errorBuilder.SetExtension("inputObjectError", ex.Message);
                    break;
                default:
                    if (exception is { })
                    {
                        errorBuilder.SetException(exception);
                    }
                    break;
            }

            return new GraphQLException(errorBuilder.Build());
        }

        public static GraphQLException MissingIfArgument(
            DirectiveNode directive)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        Resources.ThrowHelper_MissingDirectiveIfArgument,
                        directive.Name.Value)
                    .AddLocation(directive)
                    .Build());
        }

        public static GraphQLException FieldDoesNotExistOnType(
            FieldNode selection, string typeName)
        {
            return new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    Resources.ThrowHelper_FieldDoesNotExistOnType,
                    selection.Name.Value,
                    typeName)
                .AddLocation(selection)
                .Build());
        }

        public static NotSupportedException QueryTypeNotSupported() =>
            new NotSupportedException("The specified query type is not supported.");

        public static GraphQLException VariableNotFound(
            NameString variableName) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "The variable with the name `{0}` does not exist.",
                    variableName)
                .Build());

        public static GraphQLException VariableNotFound(
            VariableNode variable) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "The variable with the name `{0}` does not exist.",
                    variable.Name.Value)
                .AddLocation(variable)
                .Build());

        public static GraphQLException VariableNotOfType(
            NameString variableName,
            Type type) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "The variable with the name `{0}` is not of the requested type `{1}`.",
                    variableName,
                    type.FullName ?? string.Empty)
                .Build());

        public static GraphQLException RootTypeNotSupported(
            OperationType operationType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage("The root type `{0}` is not supported.", operationType)
                .Build());

        public static GraphQLException SubscriptionExecutor_ContextInvalidState() =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage("The request context is in an invalid state for subscriptions.")
                .Build());

        public static GraphQLException SubscriptionExecutor_SubscriptionsMustHaveOneField() =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage("Subscription queries must have exactly one root field.")
                .Build());

        public static GraphQLException SubscriptionExecutor_NoSubscribeResolver() =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage("You must declare a subscribe resolver for subscription fields.")
                .Build());

        public static GraphQLException ResolverContext_LiteralsNotSupported(
            FieldNode field, Path path, NameString argumentName, Type requestedType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "The ArgumentValue method on the resolver context only allows for runtime " +
                    "values. If you want to retrieve the argument value as GraphQL literal use " +
                    "the ArgumentLiteral method instead.")
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .SetExtension("requestedType", requestedType.FullName)
                .Build());

        public static GraphQLException ResolverContext_CannotConvertArgument(
            FieldNode field, Path path, NameString argumentName, Type requestedType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "Unable to convert the value if the argument `{0}` to `{1}`. " +
                    "Check if the requested type is correct or register a custom type converter.",
                    argumentName,
                    requestedType.FullName ?? requestedType.Name)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .SetExtension("requestedType", requestedType.FullName)
                .Build());

        public static GraphQLException ResolverContext_LiteralNotCompatible(
            FieldNode field, Path path, NameString argumentName,
            Type requestedType, Type actualType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "The argument literal representation is `{0}` which is not compatible " +
                    "with the request literal type `{1}`.",
                    actualType.FullName ?? actualType.Name,
                    requestedType.FullName ?? actualType.Name)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .SetExtension("requestedType", requestedType.FullName)
                .SetExtension("actualType", actualType.FullName)
                .Build());

        public static GraphQLException ResolverContext_ArgumentDoesNotExist(
            FieldNode field, Path path, NameString argumentName) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "There was no argument with the name `{0}` found on the field `{1}`.",
                    argumentName,
                    field.Name.Value)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .Build());
    }
}
