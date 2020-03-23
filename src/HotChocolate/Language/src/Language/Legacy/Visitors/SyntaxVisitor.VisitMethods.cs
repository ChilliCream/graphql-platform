using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class SyntaxVisitor<TStart, TContext>
    {
        protected virtual void VisitName(
            NameNode node,
            TContext context)
        { }

        protected virtual void VisitDocument(
            DocumentNode node,
            TContext context)
        { }

        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitVariableDefinition(
            VariableDefinitionNode node,
            TContext context)
        { }

        protected virtual void VisitVariable(
            VariableNode node,
            TContext context)
        { }

        protected virtual void VisitSelectionSet(
            SelectionSetNode node,
            TContext context)
        { }

        protected virtual void VisitField(
            FieldNode node,
            TContext context)
        { }

        protected virtual void VisitArgument(
            ArgumentNode node,
            TContext context)
        { }

        protected virtual void VisitFragmentSpread(
            FragmentSpreadNode node,
            TContext context)
        { }

        protected virtual void VisitInlineFragment(
            InlineFragmentNode node,
            TContext context)
        { }

        protected virtual void VisitFragmentDefinition(
            FragmentDefinitionNode node,
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

        protected virtual void VisitDirective(
            DirectiveNode node,
            TContext context)
        { }

        protected virtual void VisitNamedType(
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

        protected virtual void VisitSelection(
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

        protected virtual void VisitValue(IValueNode node, TContext context)
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

        protected virtual void VisitType(ITypeNode node, TContext context)
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

        protected static void VisitMany<T>(
            IEnumerable<T> items,
            TContext context,
            Action<T, TContext> action)
        {
            foreach (T item in items)
            {
                action(item, context);
            }
        }
    }
}
