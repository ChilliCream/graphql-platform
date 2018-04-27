using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class VariableValueResolver
    {
        public Dictionary<string, CoercedValue> CoerceVariableValues(
            ISchema schema,
            OperationDefinitionNode operation,
            IReadOnlyDictionary<string, IValueNode> variableValues)
        {
            List<QueryError> errors = new List<QueryError>();
            Dictionary<string, CoercedValue> coercedValues =
                new Dictionary<string, CoercedValue>();

            foreach (VariableDefinitionNode variableDefinition in
                operation.VariableDefinitions)
            {
                string variableName = variableDefinition.Variable.Name.Value;
                IType variableType = GetType(schema, variableDefinition.Type);
                IValueNode defaultValue = variableDefinition.DefaultValue;

                if (variableType is IInputType type)
                {
                    if (!variableValues.TryGetValue(variableName,
                        out IValueNode variableValue))
                    {
                        variableValue = defaultValue;
                    }

                    if (type.IsNonNullType() && IsNulValue(variableValue))
                    {
                        errors.Add(new VariableError(
                            "The variable value cannot be null.",
                            variableName));
                    }

                    if (!type.IsInstanceOfType(variableValue))
                    {
                        errors.Add(new VariableError(
                            "The variable value is not of the variable type.",
                            variableName));
                    }

                    coercedValues[variableName] =
                            new CoercedValue(type, variableValue);
                }
                else
                {
                    errors.Add(new VariableError(
                        $"The variable type ({variableType.ToString()}) " +
                        "must be an input object type.",
                        variableName));
                }
            }

            if (errors.Any())
            {
                throw new QueryException(errors);
            }

            return coercedValues;
        }

        private IType GetType(ISchema schema, ITypeNode typeNode)
        {
            if (typeNode is NonNullTypeNode nonNullType)
            {
                return new NonNullType(GetType(schema, nonNullType.Type));
            }

            if (typeNode is ListTypeNode listType)
            {
                return new ListType(GetType(schema, listType.Type));
            }

            if (typeNode is NamedTypeNode namedType)
            {
                return schema.GetType(namedType.Name.Value);
            }

            throw new NotSupportedException(
                "The type node kind is not supported.");
        }

        private static bool IsNulValue(IValueNode valueNode)
        {
            return valueNode == null || valueNode is NullValueNode;
        }
    }
}
