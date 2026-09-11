using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Builds the canonical, fully coerced <see cref="PolicyAction"/> for one compiled selection
/// occurrence: its action name is always the declaring type and field name, never the response
/// alias, and its arguments are reconstructed from the compiled operation and the request's coerced
/// variable values, never read from a serialized plan value.
/// </summary>
internal static class PolicyActionCoercion
{
    /// <summary>
    /// Builds the action for the given occurrence's compiled selection.
    /// </summary>
    public static PolicyAction BuildAction(
        string typeName,
        string fieldName,
        Selection selection,
        IVariableValueCollection variables)
    {
        var arguments = CoerceArguments(selection, variables);
        return new PolicyAction($"{typeName}.{fieldName}", arguments);
    }

    private static ObjectValueNode CoerceArguments(Selection selection, IVariableValueCollection variables)
    {
        var argumentNodes = selection.SyntaxNodes[0].Node.Arguments;
        var argumentDefinitions = selection.Field.Arguments;
        var fields = new List<ObjectFieldNode>(argumentDefinitions.Count);

        // Canonical order is the field's own argument-definition order (its stable Index), never
        // the order the client happened to write the arguments in the query: two occurrences of the
        // same guarded field with the same effective arguments in a different textual order must
        // coerce to the same envelope. The composite schema's own argument collection already
        // iterates in this order; the explicit sort makes that contract part of this method rather
        // than an incidental property of how the collection happens to enumerate.
        foreach (var argumentDefinition in argumentDefinitions
            .OrderBy(argument => ((FusionInputFieldDefinition)argument).Index))
        {
            if (TryFindArgument(argumentNodes, argumentDefinition.Name, out var argumentNode))
            {
                fields.Add(new ObjectFieldNode(
                    argumentDefinition.Name,
                    CoerceValue(argumentDefinition.Type, argumentNode.Value, variables)));
            }
            else if (argumentDefinition.DefaultValue is { } defaultValue)
            {
                fields.Add(new ObjectFieldNode(
                    argumentDefinition.Name,
                    CoerceValue(argumentDefinition.Type, defaultValue, variables)));
            }

            // An omitted argument with no default is absent from the envelope.
        }

        return new ObjectValueNode(fields);
    }

    private static IValueNode CoerceValue(
        IType type,
        IValueNode value,
        IVariableValueCollection variables)
    {
        if (value is VariableNode variableNode)
        {
            var resolved = variables.TryGetValue<IValueNode>(variableNode.Name.Value, out var resolvedValue)
                ? resolvedValue
                : NullValueNode.Default;
            return CoerceValue(type, resolved, variables);
        }

        if (value is NullValueNode)
        {
            return NullValueNode.Default;
        }

        var unwrappedType = type.Kind is TypeKind.NonNull ? type.InnerType() : type;

        if (value is ListValueNode listValue)
        {
            var elementType = unwrappedType.Kind is TypeKind.List
                ? ((ListType)unwrappedType).ElementType
                : unwrappedType;
            var items = new IValueNode[listValue.Items.Count];

            for (var i = 0; i < items.Length; i++)
            {
                items[i] = CoerceValue(elementType, listValue.Items[i], variables);
            }

            return new ListValueNode(items);
        }

        if (value is ObjectValueNode objectValue)
        {
            if (unwrappedType.NamedType() is not IInputObjectTypeDefinition inputType)
            {
                // Not reachable for a valid, schema-conforming operation; keep the literal as-is
                // rather than throw, since this method must never itself become a fail-open path.
                return objectValue;
            }

            var fields = new List<ObjectFieldNode>(inputType.Fields.Count);

            // Same canonical-order contract as the top-level arguments: the input type's own
            // field-definition order, never anything derived from how the literal happened to be
            // written in the query.
            foreach (var fieldDefinition in inputType.Fields
                .OrderBy(field => ((FusionInputFieldDefinition)field).Index))
            {
                if (TryFindField(objectValue.Fields, fieldDefinition.Name, out var fieldNode))
                {
                    fields.Add(new ObjectFieldNode(
                        fieldDefinition.Name,
                        CoerceValue(fieldDefinition.Type, fieldNode.Value, variables)));
                }
                else if (fieldDefinition.DefaultValue is { } defaultValue)
                {
                    fields.Add(new ObjectFieldNode(
                        fieldDefinition.Name,
                        CoerceValue(fieldDefinition.Type, defaultValue, variables)));
                }
            }

            return new ObjectValueNode(fields);
        }

        // A scalar, enum, or boolean literal is already fully resolved; a resolved variable value
        // can never itself contain a nested VariableNode.
        return value;
    }

    private static bool TryFindArgument(
        IReadOnlyList<ArgumentNode> arguments,
        string name,
        out ArgumentNode argument)
    {
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i].Name.Value.Equals(name, StringComparison.Ordinal))
            {
                argument = arguments[i];
                return true;
            }
        }

        argument = null!;
        return false;
    }

    private static bool TryFindField(
        IReadOnlyList<ObjectFieldNode> fields,
        string name,
        out ObjectFieldNode field)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i].Name.Value.Equals(name, StringComparison.Ordinal))
            {
                field = fields[i];
                return true;
            }
        }

        field = null!;
        return false;
    }
}
