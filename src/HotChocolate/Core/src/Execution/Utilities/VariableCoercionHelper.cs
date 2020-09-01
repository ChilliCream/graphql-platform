using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class VariableCoercionHelper
    {
        public void CoerceVariableValues(
            ISchema schema,
            IReadOnlyList<VariableDefinitionNode> variableDefinitions,
            IReadOnlyDictionary<string, object?> values,
            IDictionary<string, VariableValue> coercedValues)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (variableDefinitions is null)
            {
                throw new ArgumentNullException(nameof(variableDefinitions));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (coercedValues is null)
            {
                throw new ArgumentNullException(nameof(coercedValues));
            }

            for (var i = 0; i < variableDefinitions.Count; i++)
            {
                VariableDefinitionNode variableDefinition = variableDefinitions[i];
                var variableName = variableDefinition.Variable.Name.Value;
                IInputType variableType = AssertInputType(schema, variableDefinition);

                if (!values.TryGetValue(variableName, out object? value) &&
                    variableDefinition.DefaultValue is { } defaultValue)
                {
                    value = defaultValue.Kind == SyntaxKind.NullValue ? null : defaultValue;
                }

                if (value is null || value is NullValueNode)
                {
                    if (variableType.IsNonNullType())
                    {
                        throw ThrowHelper.NonNullVariableIsNull(variableDefinition);
                    }
                    coercedValues[variableName] = new VariableValue(
                        variableType, null, NullValueNode.Default);
                }
                else
                {
                    coercedValues[variableName] = CoerceVariableValue(
                        variableDefinition, variableType, value);
                }
            }
        }

        private static VariableValue CoerceVariableValue(
            VariableDefinitionNode variableDefinition,
            IInputType variableType,
            object resultValue)
        {
            if (resultValue is IValueNode valueLiteral)
            {
                try
                {
                    return new VariableValue(
                        variableType,
                        variableType.ParseLiteral(valueLiteral),
                        valueLiteral);
                }
                catch (Exception ex)
                {
                    throw ThrowHelper.VariableValueInvalidType(variableDefinition, ex);
                }
            }

            if (variableType.TryDeserialize(resultValue, out object? deserialized))
            {
                return new VariableValue(variableType, deserialized, null);
            }
            throw ThrowHelper.VariableValueInvalidType(variableDefinition);
        }

        private static IInputType AssertInputType(
            ISchema schema,
            VariableDefinitionNode variableDefinition)
        {
            if (schema.TryGetTypeFromAst(variableDefinition.Type, out IInputType type))
            {
                return type;
            }
            throw ThrowHelper.VariableIsNotAnInputType(variableDefinition);
        }
    }
}
