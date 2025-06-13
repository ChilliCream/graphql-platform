using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal sealed class VariableCoercionHelper
{
    public bool TryCoerceVariableValues(
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        IReadOnlyDictionary<string, object?> variableValues,
        [NotNullWhen(true)] out Dictionary<string, VariableValue>? coercedVariableValues,
        [NotNullWhen(false)] out IError? error)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(variableValues);

        coercedVariableValues = [];
        error = null;

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;
            var variableType = AssertInputType(schema, variableDefinition);

            var hasValue = variableValues.TryGetValue(variableName, out var value);

            if (!hasValue && variableDefinition.DefaultValue is { } defaultValue)
            {
                value = defaultValue.Kind is SyntaxKind.NullValue ? null : defaultValue;
                hasValue = true;
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

                coercedVariableValues[variableName]  = new(variableName, variableType, NullValueNode.Default);
            }
            else if (value is IValueNode valueLiteral)
            {
                if (TryCoerceVariableValue(
                    variableDefinition,
                    variableType,
                    valueLiteral,
                    out var variableValue,
                    out error))
                {
                    coercedVariableValues[variableName] = variableValue.Value;
                }
                else
                {
                    coercedVariableValues = null;
                    return false;
                }
            }
            else
            {
                throw new NotSupportedException(
                    $"The variable value of type {value?.GetType().Name} is not supported.");
            }
        }
    }

    private bool TryCoerceVariableValue(
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        IValueNode value,
        [NotNullWhen(true)] out VariableValue? variableValue,
        [NotNullWhen(false)] out IError? error)
    {
        var root = Path.Root.Append(variableDefinition.Variable.Name.Value);

        // we are ensuring here that enum values are correctly specified.
        value = Rewrite(variableType, value);

        return new VariableValue(
            variableDefinition.Variable.Name.Value,
            variableType,
            value);
    }

    private static IInputType AssertInputType(
        ISchemaDefinition schema,
        VariableDefinitionNode variableDefinition)
    {
        if (schema.Types.TryGetType(variableDefinition.Type, out IInputType? type))
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

            case StringValueNode sv when inputType.IsEnumType():
                return new EnumValueNode(sv.Location, sv.Value);

            default:
                return node;
        }
    }

    private static ObjectValueNode Rewrite(
        IType inputType,
        ObjectValueNode node)
    {
        if (inputType.NamedType() is not InputObjectType inputObjectType)
        {
            // if the node type is not an input object, we will just return the node
            // as if and the deserialization will produce a proper error.
            return node;
        }

        List<ObjectFieldNode>? fields = null;

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var current = node.Fields[i];

            if (!inputObjectType.Fields.TryGetField(current.Name.Value, out var field))
            {
                // if we do not find a field on the type we also skip this error and let
                // the deserialization produce a proper error on this.
                fields?.Add(current);
                continue;
            }

            var rewritten = Rewrite(field.Type, current.Value);

            // we try initially just to traverse the input graph, only if we detect a change
            // will we create a new input object. In this case if the fields list is initialized
            // we know that we have already collected at least one change. In this case
            // all further field nodes have to be added as well even if they do not have
            // a changed value since we need to produce a complete new input object value node.
            if (fields is not null)
            {
                fields.Add(current.WithValue(rewritten));
            }

            // if we did not so far detect any rewritten field value we will compare if the
            // field value node changed. Since, all syntax nodes are immutable we can just
            // check if the reference is not the same.
            else if (!ReferenceEquals(current.Value, rewritten))
            {
                // if we detect a reference change we will create the fields list
                // that contains all previous field values plus the changed field value.
                fields = [];

                for (var j = 0; j < i; j++)
                {
                    fields.Add(node.Fields[j]);
                }

                fields.Add(current.WithValue(rewritten));
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

        var elementType = inputType.ListType().ElementType;
        List<IValueNode>? values = null;

        for (var i = 0; i < node.Items.Count; i++)
        {
            var current = node.Items[i];
            var value = Rewrite(elementType, current);

            // we try initially just to traverse the list graph, only if we detect a change
            // will we create a new list object. In this case if values list is initialized
            // we know that we have already collected at least one change. In this case
            // all further value nodes have to be added as well even if they do not have
            // a changed value since we need to produce a complete new list value node.
            if (values is not null)
            {
                values.Add(value);
            }

            // if we did not so far detect any rewritten value we will compare if the
            // value node changed. Since, all syntax nodes are immutable we can just
            // check if the reference is not the same.
            else if (!ReferenceEquals(current, value))
            {
                // if we detect a reference change we will create the values list
                // that contains all previous list values plus the changed list value.
                values = [];

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
