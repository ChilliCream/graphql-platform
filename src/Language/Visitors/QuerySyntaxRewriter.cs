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

        protected override FieldNode VisitField(
            FieldNode node,
            TContext context)
        {
            if (node.Alias != null)
            {
                VisitName(node.Alias, context);
            }

            VisitName(node.Name, context);
            VisitMany(node.Arguments, context, VisitArgument);
            VisitMany(node.Directives, context, VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet, context);
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            TContext context)
        {
            if (node.TypeCondition != null)
            {
                VisitNamedType(node.TypeCondition, context);
            }

            VisitMany(node.Directives, context, VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet, context);
            }
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
        { }


        protected virtual void VisitArgument(
            ArgumentNode node,
            TContext context)
        { }



        protected virtual void VisitIntValue(
            IntValueNode node,
            TContext context)
        { }

        protected virtual void VisitFloatValue(
            FloatValueNode node,
            TContext context)
        { }

        protected virtual void VisitStringValue(
            StringValueNode node,
            TContext context)
        { }

        protected virtual void VisitBooleanValue(
            BooleanValueNode node,
            TContext context)
        { }

        protected virtual void VisitEnumValue(
            EnumValueNode node,
            TContext context)
        { }

        protected virtual void VisitNullValue(
            NullValueNode node,
            TContext context)
        { }

        protected virtual void VisitListValue(
            ListValueNode node,
            TContext context)
        { }
        protected virtual void VisitObjectValue(
            ObjectValueNode node,
            TContext context)
        { }

        protected virtual void VisitObjectField(
            ObjectFieldNode node,
            TContext context)
        { }

        protected virtual DirectiveNode VisitDirective(
            DirectiveNode node,
            TContext context)
        {
            return node;
        }

        protected virtual NamedTypeNode VisitNamedType(
            NamedTypeNode node,
            TContext context)
        { }

        protected virtual void VisitListType(
            ListTypeNode node,
            TContext context)
        { }

        protected virtual void VisitNonNullType(
            NonNullTypeNode node,
            TContext context)
        { }

        protected virtual void VisitSchemaDefinition(
            SchemaDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitScalarTypeDefinition(
            ScalarTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitFieldDefinition(
            FieldDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitInputValueDefinition(
            InputValueDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitEnumValueDefinition(
            EnumValueDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitSchemaExtension(
            SchemaExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitScalarTypeExtension(
            ScalarTypeExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitUnionTypeExtension(
            UnionTypeExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitEnumTypeExtension(
            EnumTypeExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            TContext context)
        { }

        protected virtual void VisitDirectiveDefinition(
            DirectiveDefinitionNode node,
            TContext context)
        { }

        protected virtual ISelectionNode VisitSelection(
            ISelectionNode node,
            TContext context)
        {
            switch (node)
            {
                case FieldNode value:
                    VisitField(value, context);
                    break;
                case FragmentSpreadNode value:
                    VisitFragmentSpread(value, context);
                    break;
                case InlineFragmentNode value:
                    VisitInlineFragment(value, context);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual IValueNode VisitValue(IValueNode node, TContext context)
        {
            if (node is null)
            {
                return;
            }

            switch (node)
            {
                case IntValueNode value:
                    VisitIntValue(value, context);
                    break;
                case FloatValueNode value:
                    VisitFloatValue(value, context);
                    break;
                case StringValueNode value:
                    VisitStringValue(value, context);
                    break;
                case BooleanValueNode value:
                    VisitBooleanValue(value, context);
                    break;
                case EnumValueNode value:
                    VisitEnumValue(value, context);
                    break;
                case NullValueNode value:
                    VisitNullValue(value, context);
                    break;
                case ListValueNode value:
                    VisitListValue(value, context);
                    break;
                case ObjectValueNode value:
                    VisitObjectValue(value, context);
                    break;
                case VariableNode value:
                    VisitVariable(value, context);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual ITypeNode VisitType(ITypeNode node, TContext context)
        {
            switch (node)
            {
                case NonNullTypeNode value:
                    VisitNonNullType(value, context);
                    break;
                case ListTypeNode value:
                    VisitListType(value, context);
                    break;
                case NamedTypeNode value:
                    VisitNamedType(value, context);
                    break;
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
}
