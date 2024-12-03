using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The variable rewriter is a utility that rewrites a value node and replaces all
/// occurrences of <see cref="VariableNode" /> with concrete <see cref="IValueNode" />.
/// </summary>
public static class VariableRewriter
{
    /// <summary>
    /// Rewrites <paramref name="node" /> and replaces all occurrences of
    /// <see cref="VariableNode" /> with concrete <see cref="IValueNode" />
    /// from the <paramref name="variableValues" />.
    /// </summary>
    /// <param name="node">
    /// The value that shall be rewritten.
    /// </param>
    /// <param name="type">
    /// The value type.
    /// </param>
    /// <param name="defaultValue">
    /// The argument default value.
    /// </param>
    /// <param name="variableValues">
    /// The variable values.
    /// </param>
    /// <returns>
    /// Returns a <see cref="IValueNode" /> that has no variables.
    /// </returns>
    public static IValueNode Rewrite(
        IValueNode node,
        IType type,
        IValueNode? defaultValue,
        IVariableValueCollection variableValues)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (variableValues is null)
        {
            throw new ArgumentNullException(nameof(variableValues));
        }

        return TryRewriteValue(
            node,
            type,
            defaultValue ?? NullValueNode.Default,
            variableValues,
            out var rewritten)
            ? rewritten
            : node;
    }

    private static ObjectValueNode Rewrite(
        ObjectValueNode node,
        InputObjectType type,
        IVariableValueCollection variableValues)
    {
        if (node.Fields.Count == 0)
        {
            return node;
        }

        if (node.Fields.Count == 1)
        {
            var oneOf = type.Directives.ContainsDirective(WellKnownDirectives.OneOf);
            var value = node.Fields[0];

            if (type.Fields.TryGetField(value.Name.Value, out var field) &&
                TryRewriteField(value, field, variableValues, out var rewritten))
            {
                if (oneOf && rewritten.Value.Kind is SyntaxKind.NullValue)
                {
                    throw ThrowHelper.OneOfFieldMustBeNonNull(field.Coordinate);
                }

                return node.WithFields(new[] { rewritten, });
            }
            else
            {
                return node;
            }
        }

        ObjectFieldNode[]? rewrittenItems = null;

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var value = node.Fields[i];

            if (type.Fields.TryGetField(value.Name.Value, out var field) &&
                TryRewriteField(value, field, variableValues, out var rewritten))
            {
                if (rewrittenItems is null)
                {
                    rewrittenItems = new ObjectFieldNode[node.Fields.Count];
                    for (var j = 0; j < node.Fields.Count; j++)
                    {
                        rewrittenItems[j] = node.Fields[j];
                    }
                }
                rewrittenItems[i] = rewritten;
            }
            else if (rewrittenItems is not null)
            {
                rewrittenItems[i] = node.Fields[i];
            }
        }

        if (rewrittenItems is not null)
        {
            return node.WithFields(rewrittenItems);
        }

        return node;
    }

    private static bool TryRewriteField(
        ObjectFieldNode original,
        InputField field,
        IVariableValueCollection variableValues,
        [NotNullWhen(true)] out ObjectFieldNode? rewritten)
    {
        if (TryRewriteValue(
            original.Value,
            field.Type,
            field.DefaultValue ?? NullValueNode.Default,
            variableValues,
            out var rewrittenValue))
        {
            rewritten = original.WithValue(rewrittenValue);
            return true;
        }

        rewritten = null;
        return false;
    }

    private static ObjectValueNode Rewrite(
        ObjectValueNode node,
        ListType type,
        IVariableValueCollection variableValues)
    {
        return TryRewriteValue(
            node,
            type.ElementType,
            NullValueNode.Default,
            variableValues,
            out var rewritten) &&
            rewritten is ObjectValueNode rewrittenObj
                ? rewrittenObj
                : node;
    }

    private static ListValueNode Rewrite(
        ListValueNode node,
        ListType type,
        IVariableValueCollection variableValues)
    {
        if (node.Items.Count == 0)
        {
            return node;
        }

        if (node.Items.Count == 1)
        {
            return TryRewriteValue(
                node.Items[0],
                type.ElementType,
                NullValueNode.Default,
                variableValues,
                out var rewritten)
                ? node.WithItems(new[] { rewritten, })
                : node;
        }

        IValueNode[]? rewrittenItems = null;

        for (var i = 0; i < node.Items.Count; i++)
        {
            if (TryRewriteValue(
                node.Items[i],
                type.ElementType,
                NullValueNode.Default,
                variableValues,
                out var rewritten))
            {
                if (rewrittenItems is null)
                {
                    rewrittenItems = new IValueNode[node.Items.Count];
                    for (var j = 0; j < i; j++)
                    {
                        rewrittenItems[j] = node.Items[j];
                    }
                }
                rewrittenItems[i] = rewritten;
            }
            else if (rewrittenItems is not null)
            {
                rewrittenItems[i] = node.Items[i];
            }
        }

        if (rewrittenItems is not null)
        {
            return node.WithItems(rewrittenItems);
        }

        return node;
    }

    private static bool TryRewriteValue(
        IValueNode original,
        IType type,
        IValueNode defaultValue,
        IVariableValueCollection variableValues,
        [NotNullWhen(true)] out IValueNode? rewritten)
    {
        if (type.Kind == TypeKind.NonNull)
        {
            type = type.InnerType();
        }

        switch (original.Kind)
        {
            case SyntaxKind.Variable:
                rewritten = Rewrite((VariableNode)original, defaultValue, variableValues);
                return true;

            case SyntaxKind.ObjectValue when type.Kind == TypeKind.InputObject:
                rewritten = Rewrite(
                    (ObjectValueNode)original,
                    (InputObjectType)type,
                    variableValues);

                if (ReferenceEquals(rewritten, original))
                {
                    rewritten = null;
                    return false;
                }
                return true;

            case SyntaxKind.ObjectValue when type.Kind == TypeKind.List:
                rewritten = Rewrite(
                    (ObjectValueNode)original,
                    (ListType)type,
                    variableValues);

                if (ReferenceEquals(rewritten, original))
                {
                    rewritten = null;
                    return false;
                }
                return true;

            case SyntaxKind.ListValue when type.Kind == TypeKind.List:
                rewritten = Rewrite(
                    (ListValueNode)original,
                    (ListType)type,
                    variableValues);

                if (ReferenceEquals(rewritten, original))
                {
                    rewritten = null;
                    return false;
                }
                return true;

            default:
                rewritten = null;
                return false;
        }
    }

    private static IValueNode Rewrite(
        VariableNode node,
        IValueNode defaultValue,
        IVariableValueCollection variableValues) =>
        variableValues.TryGetVariable(node.Name.Value, out IValueNode? value)
            ? value ?? NullValueNode.Default
            : defaultValue;
}
