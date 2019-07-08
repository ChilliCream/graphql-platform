using System.Buffers;
using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class SyntaxRewriter<TContext>
    {
        protected virtual NameNode RewriteName(
            NameNode node,
            TContext context)
        {
            return node;
        }

        protected virtual VariableNode RewriteVariable(
            VariableNode node,
            TContext context)
        {
            VariableNode current = node;

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            return current;
        }

        protected virtual ArgumentNode RewriteArgument(
            ArgumentNode node,
            TContext context)
        {
            ArgumentNode current = node;

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, node.Value, context,
                RewriteValue, current.WithValue);

            return current;
        }

        protected virtual IntValueNode RewriteIntValue(
            IntValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual FloatValueNode RewriteFloatValue(
            FloatValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual StringValueNode RewriteStringValue(
            StringValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual BooleanValueNode RewriteBooleanValue(
            BooleanValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual EnumValueNode RewriteEnumValue(
            EnumValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual NullValueNode RewriteNullValue(
            NullValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual ListValueNode RewriteListValue(
            ListValueNode node,
            TContext context)
        {
            ListValueNode current = node;

            current = RewriteMany(current, current.Items, context,
                RewriteValue, current.WithItems);

            return current;
        }

        protected virtual ObjectValueNode RewriteObjectValue(
            ObjectValueNode node,
            TContext context)
        {
            ObjectValueNode current = node;

            current = RewriteMany(current, current.Fields, context,
                RewriteObjectField, current.WithFields);

            return current;
        }

        protected virtual ObjectFieldNode RewriteObjectField(
            ObjectFieldNode node,
            TContext context)
        {
            return node;
        }

        protected virtual DirectiveNode RewriteDirective(
            DirectiveNode node,
            TContext context)
        {
            return node;
        }

        protected virtual TParent RewriteDirectives<TParent>(
            TParent parent,
            IReadOnlyList<DirectiveNode> directives,
            TContext context,
            Func<IReadOnlyList<DirectiveNode>, TParent> rewrite)
        {
            return RewriteMany(parent, directives, context,
                RewriteDirective, rewrite);
        }

        protected virtual NamedTypeNode RewriteNamedType(
            NamedTypeNode node,
            TContext context)
        {
            NamedTypeNode current = node;

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            return current;
        }

        protected virtual ListTypeNode RewriteListType(
            ListTypeNode node,
            TContext context)
        {
            ListTypeNode current = node;

            current = Rewrite(current, current.Type, context,
                RewriteType, current.WithType);

            return current;
        }

        protected virtual NonNullTypeNode RewriteNonNullType(
            NonNullTypeNode node,
            TContext context)
        {
            NonNullTypeNode current = node;

            current = Rewrite(current, current.Type, context,
                (t, c) => (INullableTypeNode)RewriteType(t, c),
                current.WithType);

            return current;
        }

        protected virtual IValueNode RewriteValue(
            IValueNode node, TContext context)
        {
            if (node is null)
            {
                return null;
            }

            switch (node)
            {
                case IntValueNode value:
                    return RewriteIntValue(value, context);

                case FloatValueNode value:
                    return RewriteFloatValue(value, context);

                case StringValueNode value:
                    return RewriteStringValue(value, context);

                case BooleanValueNode value:
                    return RewriteBooleanValue(value, context);

                case EnumValueNode value:
                    return RewriteEnumValue(value, context);

                case NullValueNode value:
                    return RewriteNullValue(value, context);

                case ListValueNode value:
                    return RewriteListValue(value, context);

                case ObjectValueNode value:
                    return RewriteObjectValue(value, context);

                case VariableNode value:
                    return RewriteVariable(value, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual ITypeNode RewriteType(
            ITypeNode node,
            TContext context)
        {
            switch (node)
            {
                case NonNullTypeNode value:
                    return RewriteNonNullType(value, context);

                case ListTypeNode value:
                    return RewriteListType(value, context);

                case NamedTypeNode value:
                    return RewriteNamedType(value, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected static TParent Rewrite<TParent, TProperty>(
            TParent parent,
            TProperty property,
            TContext context,
            Func<TProperty, TContext, TProperty> visit,
            Func<TProperty, TParent> rewrite)
            where TProperty : class
        {
            if (property is null)
            {
                return parent;
            }

            TProperty rewritten = visit(property, context);
            if (ReferenceEquals(property, rewritten))
            {
                return parent;
            }
            return rewrite(rewritten);
        }

        protected static TParent RewriteMany<TParent, TProperty>(
            TParent parent,
            IReadOnlyList<TProperty> property,
            TContext context,
            Func<TProperty, TContext, TProperty> visit,
            Func<IReadOnlyList<TProperty>, TParent> rewrite)
            where TProperty : class
        {
            return Rewrite(parent, property, context,
                (p, c) => RewriteMany(p, c, visit),
                rewrite);
        }

        protected static IReadOnlyList<T> RewriteMany<T>(
           IReadOnlyList<T> items,
           TContext context,
           Func<T, TContext, T> func)
        {
            IReadOnlyList<T> current = items;

            T[] rented = ArrayPool<T>.Shared.Rent(items.Count);
            Span<T> copy = rented;
            copy = copy.Slice(0, items.Count);
            var modified = false;

            for (int i = 0; i < items.Count; i++)
            {
                T original = items[i];
                T rewritten = func(items[i], context);

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

}
