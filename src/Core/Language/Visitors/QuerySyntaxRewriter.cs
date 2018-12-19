using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class QuerySyntaxRewriter<TContext>
    {
        protected virtual bool VisitFragmentDefinitions => true;

        public virtual ISyntaxNode Rewrite(
            ISyntaxNode node,
            TContext context)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            switch (node)
            {
                case DocumentNode document:
                    return RewriteDocument(document, context);

                case OperationDefinitionNode operation:
                    return RewriteOperationDefinition(operation, context);

                case FieldNode field:
                    return RewriteField(field, context);

                case FragmentSpreadNode spread:
                    return RewriteFragmentSpread(spread, context);

                case FragmentDefinitionNode fragment:
                    return RewriteFragmentDefinition(fragment, context);

                case InlineFragmentNode inline:
                    return RewriteInlineFragment(inline, context);

                default:
                    // TODO: Exception
                    throw new Exception();
            }
        }

        protected virtual DocumentNode RewriteDocument(
            DocumentNode node,
            TContext context)
        {
            IReadOnlyCollection<IDefinitionNode> rewrittenDefinitions =
                RewriteMany(node.Definitions, context, RewriteDefinition);

            return ReferenceEquals(node.Definitions, rewrittenDefinitions)
                ? node : node.WithDefinitions(rewrittenDefinitions);
        }

        protected virtual IDefinitionNode RewriteDefinition(
            IDefinitionNode node,
            TContext context)
        {
            switch (node)
            {
                case OperationDefinitionNode value:
                    return RewriteOperationDefinition(value, context);

                case FragmentDefinitionNode value:
                    return VisitFragmentDefinitions
                        ? RewriteFragmentDefinition(value, context)
                        : value;

                default:
                    return node;
            }
        }

        protected virtual OperationDefinitionNode RewriteOperationDefinition(
            OperationDefinitionNode node,
            TContext context)
        {
            OperationDefinitionNode current = node;

            if (node.Name != null)
            {
                current = Rewrite(current, node.Name, context,
                    RewriteName, current.WithName);

                current = Rewrite(current, node.VariableDefinitions, context,
                    (p, c) => RewriteMany(p, c, RewriteVariableDefinition),
                    current.WithVariableDefinitions);

                current = Rewrite(current, node.Directives, context,
                    (p, c) => RewriteMany(p, c, RewriteDirective),
                    current.WithDirectives);
            }

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    RewriteSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual VariableDefinitionNode RewriteVariableDefinition(
            VariableDefinitionNode node,
            TContext context)
        {
            VariableDefinitionNode current = node;

            current = Rewrite(current, node.Variable, context,
                RewriteVariable, current.WithVariable);

            current = Rewrite(current, node.Type, context,
                RewriteType, current.WithType);

            if (node.DefaultValue != null)
            {
                current = Rewrite(current, node.DefaultValue, context,
                    RewriteValue, current.WithDefaultValue);
            }

            return current;
        }

        protected virtual FragmentDefinitionNode RewriteFragmentDefinition(
            FragmentDefinitionNode node,
            TContext context)
        {
            FragmentDefinitionNode current = node;

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, node.TypeCondition, context,
                RewriteNamedType, current.WithTypeCondition);

            current = Rewrite(current, node.VariableDefinitions, context,
                (p, c) => RewriteMany(p, c, RewriteVariableDefinition),
                current.WithVariableDefinitions);

            current = Rewrite(current, node.Directives, context,
                (p, c) => RewriteMany(p, c, RewriteDirective),
                current.WithDirectives);

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    RewriteSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node,
            TContext context)
        {
            SelectionSetNode current = node;

            current = Rewrite(current, node.Selections, context,
                (p, c) => RewriteMany(p, c, RewriteSelection),
                current.WithSelections);

            return current;
        }

        protected virtual FieldNode RewriteField(
            FieldNode node,
            TContext context)
        {
            FieldNode current = node;

            if (node.Alias != null)
            {
                current = Rewrite(current, node.Alias, context,
                    RewriteName, current.WithAlias);
            }

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, node.Arguments, context,
                (p, c) => RewriteMany(p, c, RewriteArgument),
                current.WithArguments);

            current = Rewrite(current, node.Directives, context,
                (p, c) => RewriteMany(p, c, RewriteDirective),
                current.WithDirectives);


            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    RewriteSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual FragmentSpreadNode RewriteFragmentSpread(
            FragmentSpreadNode node,
            TContext context)
        {
            FragmentSpreadNode current = node;

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, node.Directives, context,
                (p, c) => RewriteMany(p, c, RewriteDirective),
                current.WithDirectives);

            return current;
        }

        protected virtual InlineFragmentNode RewriteInlineFragment(
            InlineFragmentNode node,
            TContext context)
        {
            InlineFragmentNode current = node;

            if (node.TypeCondition != null)
            {
                current = Rewrite(current, node.TypeCondition, context,
                    RewriteNamedType, current.WithTypeCondition);
            }

            current = Rewrite(current, node.Directives, context,
                (p, c) => RewriteMany(p, c, RewriteDirective),
                current.WithDirectives);

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    RewriteSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

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

            return node;
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

            return node;
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
            return node;
        }

        protected virtual ObjectValueNode RewriteObjectValue(
            ObjectValueNode node,
            TContext context)
        {
            return node;
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

        protected virtual NamedTypeNode RewriteNamedType(
            NamedTypeNode node,
            TContext context)
        {
            NamedTypeNode current = node;

            current = Rewrite(current, node.Name, context,
                RewriteName, current.WithName);

            return node;
        }

        protected virtual ListTypeNode RewriteListType(
            ListTypeNode node,
            TContext context)
        {
            return node;
        }

        protected virtual NonNullTypeNode RewriteNonNullType(
            NonNullTypeNode node,
            TContext context)
        {
            return node;
        }

        protected virtual ISelectionNode RewriteSelection(
            ISelectionNode node,
            TContext context)
        {
            switch (node)
            {
                case FieldNode value:
                    return RewriteField(value, context);

                case FragmentSpreadNode value:
                    return RewriteFragmentSpread(value, context);

                case InlineFragmentNode value:
                    return RewriteInlineFragment(value, context);

                default:
                    throw new NotSupportedException();
            }
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

        protected virtual ITypeNode RewriteType(ITypeNode node, TContext context)
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
            Func<TProperty, TParent> rewrite
        )
        {
            TProperty rewritten = visit(property, context);
            if (ReferenceEquals(property, rewritten))
            {
                return parent;
            }
            return rewrite(rewritten);
        }

        protected static IReadOnlyCollection<T> RewriteMany<T>(
           IReadOnlyCollection<T> items,
           TContext context,
           Func<T, TContext, T> func)
        {
            var originalSet = new HashSet<T>(items);
            var rewrittenSet = new List<T>();
            var modified = false;

            foreach (T item in items)
            {
                T rewritten = func(item, context);
                if (!modified && !originalSet.Contains(rewritten))
                {
                    modified = true;
                }
                rewrittenSet.Add(rewritten);
            }

            return modified ? rewrittenSet : items;
        }
    }

    public static class DocumentRewriterExtensions
    {
        public static DocumentNode Rewrite<TRewriter, TContext>(
            this DocumentNode node, TContext context)
            where TRewriter : QuerySyntaxRewriter<TContext>, new()
        {
            var rewriter = new TRewriter();
            return (DocumentNode)rewriter.Rewrite(node, context);
        }

        public static T Rewrite<T, TContext>(
            this QuerySyntaxRewriter<TContext> rewriter,
            T node,
            TContext context)
            where T : ISyntaxNode
        {
            return (T)rewriter.Rewrite(node, context);
        }
    }
}
