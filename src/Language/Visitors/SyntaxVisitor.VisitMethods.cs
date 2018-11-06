using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class SyntaxVisitor<TStart>
    {
        protected virtual void VisitName(NameNode node) { }
        protected virtual void VisitDocument(DocumentNode node) { }
        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode node)
        { }
        protected virtual void VisitVariableDefinition(
            VariableDefinitionNode node)
        { }
        protected virtual void VisitVariable(VariableNode node) { }
        protected virtual void VisitSelectionSet(SelectionSetNode node) { }
        protected virtual void VisitField(FieldNode node) { }
        protected virtual void VisitArgument(ArgumentNode node) { }
        protected virtual void VisitFragmentSpread(FragmentSpreadNode node) { }
        protected virtual void VisitInlineFragment(InlineFragmentNode node) { }
        protected virtual void VisitFragmentDefinition(
            FragmentDefinitionNode node)
        { }
        protected virtual void VisitIntValue(IntValueNode node) { }
        protected virtual void VisitFloatValue(FloatValueNode node) { }
        protected virtual void VisitStringValue(StringValueNode node) { }
        protected virtual void VisitBooleanValue(BooleanValueNode node) { }
        protected virtual void VisitEnumValue(EnumValueNode node) { }
        protected virtual void VisitNullValue(NullValueNode node) { }
        protected virtual void VisitListValue(ListValueNode node) { }
        protected virtual void VisitObjectValue(ObjectValueNode node) { }
        protected virtual void VisitObjectField(ObjectFieldNode node) { }
        protected virtual void VisitDirective(DirectiveNode node) { }
        protected virtual void VisitNamedType(NamedTypeNode node) { }
        protected virtual void VisitListType(ListTypeNode node) { }
        protected virtual void VisitNonNullType(NonNullTypeNode node) { }
        protected virtual void VisitSchemaDefinition(
            SchemaDefinitionNode node)
        { }
        protected virtual void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node)
        { }
        protected virtual void VisitScalarTypeDefinition(
            ScalarTypeDefinitionNode node)
        { }
        protected virtual void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node)
        { }
        protected virtual void VisitFieldDefinition(
            FieldDefinitionNode node)
        { }
        protected virtual void VisitInputValueDefinition(
            InputValueDefinitionNode node)
        { }
        protected virtual void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node)
        { }
        protected virtual void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node)
        { }
        protected virtual void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node)
        { }
        protected virtual void VisitEnumValueDefinition(
            EnumValueDefinitionNode node)
        { }
        protected virtual void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node)
        { }
        protected virtual void VisitSchemaExtension(
            SchemaExtensionNode node)
        { }
        protected virtual void VisitScalarTypeExtension(
            ScalarTypeExtensionNode node)
        { }
        protected virtual void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node)
        { }
        protected virtual void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node)
        { }
        protected virtual void VisitUnionTypeExtension(
            UnionTypeExtensionNode node)
        { }
        protected virtual void VisitEnumTypeExtension(
            EnumTypeExtensionNode node)
        { }
        protected virtual void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node)
        { }
        protected virtual void VisitDirectiveDefinition(
            DirectiveDefinitionNode node)
        { }

        protected virtual void VisitSelection(ISelectionNode node)
        {
            switch (node)
            {
                case FieldNode value:
                    VisitField(value);
                    break;
                case FragmentSpreadNode value:
                    VisitFragmentSpread(value);
                    break;
                case InlineFragmentNode value:
                    VisitInlineFragment(value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual void VisitValue(IValueNode node)
        {
            if (node is null)
            {
                return;
            }

            switch (node)
            {
                case IntValueNode value:
                    VisitIntValue(value);
                    break;
                case FloatValueNode value:
                    VisitFloatValue(value);
                    break;
                case StringValueNode value:
                    VisitStringValue(value);
                    break;
                case BooleanValueNode value:
                    VisitBooleanValue(value);
                    break;
                case EnumValueNode value:
                    VisitEnumValue(value);
                    break;
                case NullValueNode value:
                    VisitNullValue(value);
                    break;
                case ListValueNode value:
                    VisitListValue(value);
                    break;
                case ObjectValueNode value:
                    VisitObjectValue(value);
                    break;
                case VariableNode value:
                    VisitVariable(value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual void VisitType(ITypeNode node)
        {
            switch (node)
            {
                case NonNullTypeNode value:
                    VisitNonNullType(value);
                    break;
                case ListTypeNode value:
                    VisitListType(value);
                    break;
                case NamedTypeNode value:
                    VisitNamedType(value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected static void VisitMany<T>(
            IEnumerable<T> items,
            Action<T> action)
        {
            foreach (T item in items)
            {
                action(item);
            }
        }
    }
}
