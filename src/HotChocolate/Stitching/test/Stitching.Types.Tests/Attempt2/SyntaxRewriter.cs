using System;
using System.Buffers;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class SyntaxRewriter<TContext>
{
    protected virtual NameNode RewriteName(
        NameNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual VariableNode RewriteVariable(
        VariableNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        VariableNode current = node;

        current = Rewrite(current, node.Name, navigator, context,
            RewriteName, current.WithName);

        return current;
    }

    protected virtual ArgumentNode RewriteArgument(
        ArgumentNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ArgumentNode current = node;

        current = Rewrite(current, node.Name, navigator, context,
            RewriteName, current.WithName);

        current = Rewrite(current, node.Value, navigator, context,
            RewriteValue, current.WithValue);

        return current;
    }

    protected virtual IntValueNode RewriteIntValue(
        IntValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual FloatValueNode RewriteFloatValue(
        FloatValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual StringValueNode RewriteStringValue(
        StringValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual BooleanValueNode RewriteBooleanValue(
        BooleanValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual EnumValueNode RewriteEnumValue(
        EnumValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual NullValueNode RewriteNullValue(
        NullValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual ListValueNode RewriteListValue(
        ListValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ListValueNode current = node;

        current = RewriteMany(current, current.Items, navigator, context,
            RewriteValue, current.WithItems);

        return current;
    }

    protected virtual ObjectValueNode RewriteObjectValue(
        ObjectValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ObjectValueNode current = node;

        current = RewriteMany(current, current.Fields, navigator, context,
            RewriteObjectField, current.WithFields);

        return current;
    }

    protected virtual ObjectFieldNode RewriteObjectField(
        ObjectFieldNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ObjectFieldNode current = node;

        current = Rewrite(current, node.Name, navigator, context,
            RewriteName, current.WithName);

        current = Rewrite(current, node.Value, navigator, context,
            RewriteValue, current.WithValue);

        return current;
    }

    protected virtual DirectiveNode RewriteDirective(
        DirectiveNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        return node;
    }

    protected virtual TParent RewriteDirectives<TParent>(
        TParent parent,
        IReadOnlyList<DirectiveNode> directives,
        SyntaxNavigator navigator,
        TContext context,
        Func<IReadOnlyList<DirectiveNode>, TParent> rewrite)
        where TParent : ISyntaxNode
    {
        return RewriteMany(parent, directives, navigator, context,
            RewriteDirective, rewrite);
    }

    protected virtual NamedTypeNode RewriteNamedType(
        NamedTypeNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        NamedTypeNode current = node;

        current = Rewrite(current,
            node.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        return current;
    }

    protected virtual ListTypeNode RewriteListType(
        ListTypeNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ListTypeNode current = node;

        current = Rewrite(current,
            current.Type,
            navigator,
            context,
            RewriteType,
            current.WithType);

        return current;
    }

    protected virtual NonNullTypeNode RewriteNonNullType(
        NonNullTypeNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        NonNullTypeNode current = node;

        current = Rewrite(current, current.Type, navigator, context,
            (t, n, c) => (INullableTypeNode)RewriteType(t, n, c),
            current.WithType);

        return current;
    }

    protected virtual IValueNode RewriteValue(
        IValueNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        using IDisposable _ = navigator.Push(node);

        switch (node)
        {
            case IntValueNode value:
                return RewriteIntValue(value, navigator, context);

            case FloatValueNode value:
                return RewriteFloatValue(value, navigator, context);

            case StringValueNode value:
                return RewriteStringValue(value, navigator, context);

            case BooleanValueNode value:
                return RewriteBooleanValue(value, navigator, context);

            case EnumValueNode value:
                return RewriteEnumValue(value, navigator, context);

            case NullValueNode value:
                return RewriteNullValue(value, navigator, context);

            case ListValueNode value:
                return RewriteListValue(value, navigator, context);

            case ObjectValueNode value:
                return RewriteObjectValue(value, navigator, context);

            case VariableNode value:
                return RewriteVariable(value, navigator, context);

            default:
                throw new NotSupportedException();
        }
    }

    protected virtual ITypeNode RewriteType(
        ITypeNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        using IDisposable _ = navigator.Push(node);

        switch (node)
        {
            case NonNullTypeNode value:
                return RewriteNonNullType(value, navigator, context);

            case ListTypeNode value:
                return RewriteListType(value, navigator, context);

            case NamedTypeNode value:
                return RewriteNamedType(value, navigator, context);

            default:
                throw new NotSupportedException();
        }
    }

    protected static TParent Rewrite<TParent, TProperty>(
        TParent parent,
        TProperty? property,
        SyntaxNavigator navigator,
        TContext context,
        Func<TProperty, SyntaxNavigator, TContext, TProperty> visit,
        Func<TProperty, TParent> rewrite)
        where TParent : ISyntaxNode
        where TProperty : class
    {
        if (property is null)
        {
            return parent;
        }

        TProperty rewritten = visit(property, navigator, context);
        if (ReferenceEquals(property, rewritten))
        {
            return parent;
        }
        return rewrite(rewritten);
    }

    protected static TParent RewriteMany<TParent, TProperty>(
        TParent parent,
        IReadOnlyList<TProperty> property,
        SyntaxNavigator navigator,
        TContext context,
        Func<TProperty, SyntaxNavigator, TContext, TProperty> visit,
        Func<IReadOnlyList<TProperty>, TParent> rewrite)
        where TProperty : ISyntaxNode
        where TParent : ISyntaxNode
    {
        return Rewrite(parent, property, navigator, context,
            (p, n, c) => RewriteMany(p, n, c, visit),
            rewrite);
    }

    protected static IReadOnlyList<T> RewriteMany<T>(
        IReadOnlyList<T> items,
        SyntaxNavigator navigator,
        TContext context,
        Func<T, SyntaxNavigator, TContext, T> func)
        where T : ISyntaxNode
    {
        IReadOnlyList<T> current = items;

        T[] rented = ArrayPool<T>.Shared.Rent(items.Count);
        Span<T> copy = rented;
        copy = copy.Slice(0, items.Count);
        var modified = false;

        for (int i = 0; i < items.Count; i++)
        {
            T original = items[i];

            using IDisposable _ = navigator.Push(original);

            T rewritten = func(original, navigator, context);

            copy[i] = rewritten;

            if (!modified && !ReferenceEquals(original, rewritten))
            {
                modified = true;
            }
        }

        if (modified)
        {
            var rewrittenList = new T[items.Count];

            for (int i = 0; i < items.Count; i++)
            {
                rewrittenList[i] = copy[i];
            }

            current = rewrittenList;
        }

        copy.Clear();
        ArrayPool<T>.Shared.Return(rented);

        return current;
    }
}
