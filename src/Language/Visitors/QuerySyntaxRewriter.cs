using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class QuerySyntaxRewriter<TContext>
    {
        protected virtual bool VisitFragmentDefinitions => true;

        public virtual ISyntaxNode Visit(
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
                    return VisitDocument(document, context);

                case OperationDefinitionNode operation:
                    return VisitOperationDefinition(operation, context);

                case FieldNode field:
                    return VisitField(field, context);

                case FragmentSpreadNode spread:
                    return VisitFragmentSpread(spread, context);

                case FragmentDefinitionNode fragment:
                    return VisitFragmentDefinition(fragment, context);

                case InlineFragmentNode inline:
                    return VisitInlineFragment(inline, context);

                default:
                    // TODO: Exception
                    throw new Exception();
            }
        }

        protected virtual DocumentNode VisitDocument(
            DocumentNode node,
            TContext context)
        {
            IReadOnlyCollection<IDefinitionNode> rewrittenDefinitions =
                VisitMany(node.Definitions, context, VisitDefinition);

            return ReferenceEquals(node.Definitions, rewrittenDefinitions)
                ? node : node.WithDefinitions(rewrittenDefinitions);
        }

        protected virtual IDefinitionNode VisitDefinition(
            IDefinitionNode node,
            TContext context)
        {
            switch (node)
            {
                case OperationDefinitionNode value:
                    return VisitOperationDefinition(value, context);

                case FragmentDefinitionNode value:
                    return VisitFragmentDefinitions
                        ? VisitFragmentDefinition(value, context)
                        : value;

                default:
                    return node;
            }
        }

        protected virtual OperationDefinitionNode VisitOperationDefinition(
            OperationDefinitionNode node,
            TContext context)
        {
            var current = node;

            if (node.Name != null)
            {
                current = Rewrite(current, node.Name, context,
                    VisitName, current.WithName);

                current = Rewrite(current, node.VariableDefinitions, context,
                    (p, c) => VisitMany(p, c, VisitVariableDefinition),
                    current.WithVariableDefinitions);

                current = Rewrite(current, node.Directives, context,
                    (p, c) => VisitMany(p, c, VisitDirective),
                    current.WithDirectives);
            }

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    VisitSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual VariableDefinitionNode VisitVariableDefinition(
            VariableDefinitionNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Variable, context,
                VisitVariable, current.WithVariable);

            current = Rewrite(current, node.Type, context,
                VisitType, current.WithType);

            if (node.DefaultValue != null)
            {
                current = Rewrite(current, node.DefaultValue, context,
                    VisitValue, current.WithDefaultValue);
            }

            return current;
        }

        protected virtual FragmentDefinitionNode VisitFragmentDefinition(
            FragmentDefinitionNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Name, context,
                VisitName, current.WithName);

            current = Rewrite(current, node.TypeCondition, context,
                VisitNamedType, current.WithTypeCondition);

            current = Rewrite(current, node.VariableDefinitions, context,
                (p, c) => VisitMany(p, c, VisitVariableDefinition),
                current.WithVariableDefinitions);

            current = Rewrite(current, node.Directives, context,
                (p, c) => VisitMany(p, c, VisitDirective),
                current.WithDirectives);

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    VisitSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual SelectionSetNode VisitSelectionSet(
            SelectionSetNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Selections, context,
                (p, c) => VisitMany(p, c, VisitSelection),
                current.WithSelections);

            return current;
        }

        protected virtual FieldNode VisitField(
            FieldNode node,
            TContext context)
        {
            var current = node;

            if (node.Alias != null)
            {
                current = Rewrite(current, node.Alias, context,
                    VisitName, current.WithAlias);
            }

            current = Rewrite(current, node.Name, context,
                VisitName, current.WithName);

            current = Rewrite(current, node.Arguments, context,
                (p, c) => VisitMany(p, c, VisitArgument),
                current.WithArguments);

            current = Rewrite(current, node.Directives, context,
                (p, c) => VisitMany(p, c, VisitDirective),
                current.WithDirectives);


            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    VisitSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual FragmentSpreadNode VisitFragmentSpread(
            FragmentSpreadNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Name, context,
                VisitName, current.WithName);

            current = Rewrite(current, node.Directives, context,
                (p, c) => VisitMany(p, c, VisitDirective),
                current.WithDirectives);

            return current;
        }

        protected virtual InlineFragmentNode VisitInlineFragment(
            InlineFragmentNode node,
            TContext context)
        {
            var current = node;

            if (node.TypeCondition != null)
            {
                current = Rewrite(current, node.TypeCondition, context,
                    VisitNamedType, current.WithTypeCondition);
            }

            current = Rewrite(current, node.Directives, context,
                (p, c) => VisitMany(p, c, VisitDirective),
                current.WithDirectives);

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, context,
                    VisitSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected virtual NameNode VisitName(
            NameNode node,
            TContext context)
        {
            return node;
        }


        protected virtual VariableNode VisitVariable(
            VariableNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Name, context,
                VisitName, current.WithName);

            return node;
        }


        protected virtual ArgumentNode VisitArgument(
            ArgumentNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Name, context,
                VisitName, current.WithName);

            current = Rewrite(current, node.Value, context,
                VisitValue, current.WithValue);

            return node;
        }

        protected virtual IntValueNode VisitIntValue(
            IntValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual FloatValueNode VisitFloatValue(
            FloatValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual StringValueNode VisitStringValue(
            StringValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual BooleanValueNode VisitBooleanValue(
            BooleanValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual EnumValueNode VisitEnumValue(
            EnumValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual NullValueNode VisitNullValue(
            NullValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual ListValueNode VisitListValue(
            ListValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual ObjectValueNode VisitObjectValue(
            ObjectValueNode node,
            TContext context)
        {
            return node;
        }

        protected virtual ObjectFieldNode VisitObjectField(
            ObjectFieldNode node,
            TContext context)
        {
            return node;
        }

        protected virtual DirectiveNode VisitDirective(
            DirectiveNode node,
            TContext context)
        {
            return node;
        }

        protected virtual NamedTypeNode VisitNamedType(
            NamedTypeNode node,
            TContext context)
        {
            var current = node;

            current = Rewrite(current, node.Name, context,
                VisitName, current.WithName);

            return node;
        }

        protected virtual ListTypeNode VisitListType(
            ListTypeNode node,
            TContext context)
        {
            return node;
        }

        protected virtual NonNullTypeNode VisitNonNullType(
            NonNullTypeNode node,
            TContext context)
        {
            return node;
        }

        protected virtual ISelectionNode VisitSelection(
            ISelectionNode node,
            TContext context)
        {
            switch (node)
            {
                case FieldNode value:
                    return VisitField(value, context);

                case FragmentSpreadNode value:
                    return VisitFragmentSpread(value, context);

                case InlineFragmentNode value:
                    return VisitInlineFragment(value, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual IValueNode VisitValue(
            IValueNode node, TContext context)
        {
            if (node is null)
            {
                return null;
            }

            switch (node)
            {
                case IntValueNode value:
                    return VisitIntValue(value, context);

                case FloatValueNode value:
                    return VisitFloatValue(value, context);

                case StringValueNode value:
                    return VisitStringValue(value, context);

                case BooleanValueNode value:
                    return VisitBooleanValue(value, context);

                case EnumValueNode value:
                    return VisitEnumValue(value, context);

                case NullValueNode value:
                    return VisitNullValue(value, context);

                case ListValueNode value:
                    return VisitListValue(value, context);

                case ObjectValueNode value:
                    return VisitObjectValue(value, context);

                case VariableNode value:
                    return VisitVariable(value, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual ITypeNode VisitType(ITypeNode node, TContext context)
        {
            switch (node)
            {
                case NonNullTypeNode value:
                    return VisitNonNullType(value, context);

                case ListTypeNode value:
                    return VisitListType(value, context);

                case NamedTypeNode value:
                    return VisitNamedType(value, context);

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

        protected static IReadOnlyCollection<T> VisitMany<T>(
           IReadOnlyCollection<T> items,
           TContext context,
           Func<T, TContext, T> func)
        {
            var originalSet = new HashSet<T>(items);
            var rewrittenSet = new List<T>();
            bool modified = false;

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
            return (DocumentNode)rewriter.Visit(node, context);
        }

    }
}
