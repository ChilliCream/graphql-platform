using System;
using HotChocolate.Language;

namespace HotChocolate.Execution.Utilities
{
    public static class ThrowHelper
    {
        public static GraphQLException VariableIsNotAnInputType(
            VariableDefinitionNode variableDefinition)
        {
            // throw helper
            return new GraphQLException("Variable is not an input type.");
        }

        public static GraphQLException NonNullVariableIsNull(
            VariableDefinitionNode variableDefinition)
        {
            // throw helper
            return new GraphQLException("");
        }

        public static GraphQLException VariableValueInvalidType(
            VariableDefinitionNode variableDefinition,
            Exception? exception = null)
        {
            return new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "Variable `{0}` got invalid value.",
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.InvalidType)
                .SetExtension("variable_name", variableDefinition.Variable.Name.Value)
                .SetException(exception)
                .AddLocation(variableDefinition)
                .Build());
        }
    }
}
