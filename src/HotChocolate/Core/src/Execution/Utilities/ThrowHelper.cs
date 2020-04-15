using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public static class ThrowHelper
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
                    .SetExtension("type", variableDefinition.Type.ToString())
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
                    errorBuilder.SetException(exception);
                    break;
            }

            return new GraphQLException(errorBuilder.Build());
        }
    }
}
