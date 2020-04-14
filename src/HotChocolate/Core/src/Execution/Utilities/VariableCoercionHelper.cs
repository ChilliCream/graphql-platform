using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    public class VariableCoercionHelper
    {
        private readonly ITypeConversion _typeConverter;

        public VariableCoercionHelper(ITypeConversion typeConverter)
        {
            _typeConverter = typeConverter;
        }

        public void CoerceVariableValues(
            ISchema schema,
            IReadOnlyList<VariableDefinitionNode> variableDefinitions,
            IReadOnlyDictionary<string, object?> values,
            IDictionary<string, VariableValue> coercedValues)
        {
            for (int i = 0; i < variableDefinitions.Count; i++)
            {
                VariableDefinitionNode variableDefinition = variableDefinitions[i];
                string variableName = variableDefinition.Variable.Name.Value;
                IInputType variableType = AssertInputType(schema, variableDefinition);

                if (!values.TryGetValue(variableName, out object? value) &&
                    variableDefinition.DefaultValue is { } defaultValue)
                {
                    value = defaultValue.Kind == NodeKind.NullValue ? null : defaultValue;
                }

                if (value is null)
                {
                    if (variableType.IsNonNullType())
                    {
                        throw ThrowHelper.NonNullVariableIsNull(variableDefinition);
                    }
                    coercedValues[variableName] = new VariableValue(
                        variableType, value, NullValueNode.Default);
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
            object value)
        {
            if (value is IValueNode valueLiteral)
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
            else
            {
                if (variableType.TryDeserialize(value, out object? deserialized))
                {
                    return new VariableValue(variableType, deserialized, null);
                }
                throw ThrowHelper.VariableValueInvalidType(variableDefinition);
            }
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
