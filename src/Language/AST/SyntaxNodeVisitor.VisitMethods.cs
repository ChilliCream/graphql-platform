namespace HotChocolate.Language
{
    public partial class SyntaxNodeVisitor
    {
        protected virtual void VisitName(NameNode node) { }
        protected virtual void VisitDocument(DocumentNode node) { }
        protected virtual void VisitOperationDefinition(OperationDefinitionNode node) { }
        protected virtual void VisitVariableDefinition(VariableDefinitionNode node) { }
        protected virtual void VisitVariable(VariableNode node) { }
        protected virtual void VisitSelectionSet(SelectionSetNode node) { }
        protected virtual void VisitField(FieldNode node) { }
        protected virtual void VisitArgument(ArgumentNode node) { }
        protected virtual void VisitFragmentSpread(FragmentSpreadNode node) { }
        protected virtual void VisitInlineFragment(InlineFragmentNode node) { }
        protected virtual void VisitFragmentDefinition(FragmentDefinitionNode node) { }
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
        protected virtual void VisitSchemaDefinition(SchemaDefinitionNode node) { }
        protected virtual void VisitOperationTypeDefinition(OperationTypeDefinitionNode node) { }
        protected virtual void VisitScalarTypeDefinition(ScalarTypeDefinitionNode node) { }
        protected virtual void VisitObjectTypeDefinition(ObjectTypeDefinitionNode node) { }
        protected virtual void VisitFieldDefinition(FieldDefinitionNode node) { }
        protected virtual void VisitInputValueDefinition(InputValueDefinitionNode node) { }
        protected virtual void VisitInterfaceTypeDefinition(InterfaceTypeDefinitionNode node) { }
        protected virtual void VisitUnionTypeDefinition(UnionTypeDefinitionNode node) { }
        protected virtual void VisitEnumTypeDefinition(EnumTypeDefinitionNode node) { }
        protected virtual void VisitEnumValueDefinition(EnumValueDefinitionNode node) { }
        protected virtual void VisitInputObjectTypeDefinition(InputObjectTypeDefinitionNode node) { }
        protected virtual void VisitScalarTypeExtension(ScalarTypeExtensionNode node) { }
        protected virtual void VisitObjectTypeExtension(ObjectTypeExtensionNode node) { }
        protected virtual void VisitInterfaceTypeExtension(InterfaceTypeExtensionNode node) { }
        protected virtual void VisitUnionTypeExtension(UnionTypeExtensionNode node) { }
        protected virtual void VisitEnumTypeExtension(EnumTypeExtensionNode node) { }
        protected virtual void VisitInputObjectTypeExtension(InputObjectTypeExtensionNode node) { }
        protected virtual void VisitDirectiveDefinition(DirectiveDefinitionNode node) { }
    }
}