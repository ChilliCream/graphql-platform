using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    internal sealed class VariableCoercionHelper
    {
        private readonly InputFormatter _inputFormatter;
        private readonly InputParser _inputParser;

        public VariableCoercionHelper(InputFormatter inputFormatter, InputParser inputParser)
        {
            _inputFormatter = inputFormatter ??
                throw new ArgumentNullException(nameof(inputFormatter));
            _inputParser = inputParser ??
                throw new ArgumentNullException(nameof(inputParser));
        }

        public void CoerceVariableValues(
            ISchema schema,
            IReadOnlyList<VariableDefinitionNode> variableDefinitions,
            IReadOnlyDictionary<string, object?> values,
            IDictionary<string, VariableValueOrLiteral> coercedValues)
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
                VariableValueOrLiteral coercedVariable;

                var hasValue = values.TryGetValue(variableName, out var value);

                if (!hasValue && variableDefinition.DefaultValue is { } defaultValue)
                {
                    value = defaultValue.Kind == SyntaxKind.NullValue ? null : defaultValue;
                }

                if (!hasValue || value is null || value is NullValueNode)
                {
                    if (variableType.IsNonNullType())
                    {
                        throw ThrowHelper.NonNullVariableIsNull(variableDefinition);
                    }

                    // if we do not have any value we will not create an entry to the
                    // coerced variables.
                    if (!hasValue)
                    {
                        continue;
                    }

                    coercedVariable = new(variableType, null, NullValueNode.Default);
                }
                else
                {
                    coercedVariable = CoerceVariableValue(variableDefinition, variableType, value);
                }

                coercedValues[variableName] = coercedVariable;
            }
        }

        private VariableValueOrLiteral CoerceVariableValue(
            VariableDefinitionNode variableDefinition,
            IInputType variableType,
            object value)
        {
            Path root = Path.New(variableDefinition.Variable.Name.Value);

            if (value is IValueNode valueLiteral)
            {
                try
                {
                    // we are ensuring here that enum values are correctly specified.
                    valueLiteral = Rewrite(variableType, valueLiteral);

                    return new VariableValueOrLiteral(
                        variableType,
                        _inputParser.ParseLiteral(valueLiteral, variableType, root),
                        valueLiteral);
                }
                catch (GraphQLException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw ThrowHelper.VariableValueInvalidType(variableDefinition, ex);
                }
            }

            var runtimeValue = _inputParser.ParseResult(value, variableType, root);
            IValueNode literal = _inputFormatter.FormatResult(value, variableType, root);
            return new VariableValueOrLiteral(variableType, runtimeValue, literal);
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

        private static IValueNode Rewrite(
            IType inputType,
            IValueNode node)
        {
            switch (node)
            {
                case ObjectValueNode ov:
                    return Rewrite(inputType, ov);

                case ListValueNode lv:
                    return Rewrite(inputType, lv);

                case StringValueNode sv:
                    return inputType.Kind is TypeKind.Enum
                        ? new EnumValueNode(sv.Location, sv.Value)
                        : node;

                default:
                    return node;
            }
        }

        private static ObjectValueNode Rewrite(
            IType inputType,
            ObjectValueNode node)
        {
            if (!(inputType.NamedType() is InputObjectType inputObjectType))
            {
                return node;
            }

            List<ObjectFieldNode>? fields = null;

            for (var i = 0; i < node.Fields.Count; i++)
            {
                ObjectFieldNode current = node.Fields[i];

                if (!inputObjectType.Fields.TryGetField(current.Name.Value, out IInputField? field))
                {
                    continue;
                }

                IValueNode value = Rewrite(field.Type, current.Value);

                if (fields is not null)
                {
                    fields.Add(current.WithValue(value));
                }
                else if (!ReferenceEquals(current.Value, value))
                {
                    fields = new List<ObjectFieldNode>();

                    for (var j = 0; j < i; j++)
                    {
                        fields.Add(node.Fields[j]);
                    }

                    fields.Add(current.WithValue(value));
                }
            }

            return fields is not null ? node.WithFields(fields) : node;
        }

        private static ListValueNode Rewrite(IType inputType, ListValueNode node)
        {
            if (!inputType.IsListType())
            {
                return node;
            }

            IType elementType = inputType.ListType().ElementType;
            List<IValueNode>? values = null;

            for (var i = 0; i < node.Items.Count; i++)
            {
                IValueNode current = node.Items[i];
                IValueNode value = Rewrite(elementType, current);

                if (values is not null)
                {
                    values.Add(value);
                }
                else if (!ReferenceEquals(current.Value, value))
                {
                    values = new List<IValueNode>();

                    for (var j = 0; j < i; j++)
                    {
                        values.Add(node.Items[j]);
                    }

                    values.Add(value);
                }
            }

            return values is not null ? node.WithItems(values) : node;
        }
    }
}
