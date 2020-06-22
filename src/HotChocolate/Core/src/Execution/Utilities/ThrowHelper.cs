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
    }
}
