using System;

namespace HotChocolate.Language.Utilities
{
    public sealed partial class SyntaxSerializer
    {
        private bool _indented;

        public SyntaxSerializer(SyntaxSerializerOptions options = default)
        {
            _indented = options.Indented;
        }

        public void Serialize(ISyntaxNode node, ISyntaxWriter writer)
        {
            switch (node.Kind)
            {
                case NodeKind.Name:
                    writer.WriteName((NameNode)node);
                    break;
                case NodeKind.Document:
                    VisitDocument((DocumentNode)node, writer);
                    break;
                case NodeKind.OperationDefinition:
                    VisitOperationDefinition((OperationDefinitionNode)node, writer);
                    break;
                case NodeKind.VariableDefinition:
                    VisitVariableDefinition((VariableDefinitionNode)node, writer);
                    break;
                case NodeKind.Variable:
                    writer.WriteVariable((VariableNode)node);
                    break;
                case NodeKind.SelectionSet:
                    VisitSelectionSet((SelectionSetNode)node, writer);
                    break;
                case NodeKind.Field:
                    VisitField((FieldNode)node, writer);
                    break;
                case NodeKind.Argument:
                    writer.WriteArgument((ArgumentNode)node);
                    break;
                case NodeKind.FragmentSpread:
                    VisitFragmentSpread((FragmentSpreadNode)node, writer);
                    break;
                case NodeKind.InlineFragment:
                    VisitInlineFragment((InlineFragmentNode)node, writer);
                    break;
                case NodeKind.FragmentDefinition:
                    VisitFragmentDefinition((FragmentDefinitionNode)node, writer);
                    break;
                case NodeKind.Directive:
                    writer.WriteDirective((DirectiveNode)node);
                    break;
                case NodeKind.NamedType:
                case NodeKind.ListType:
                case NodeKind.NonNullType:
                    writer.WriteType((ITypeNode)node);
                    break;
                case NodeKind.ListValue:
                case NodeKind.ObjectValue:
                case NodeKind.BooleanValue:
                case NodeKind.EnumValue:
                case NodeKind.FloatValue:
                case NodeKind.IntValue:
                case NodeKind.NullValue:
                case NodeKind.StringValue:
                    writer.WriteValue((IValueNode)node);
                    break;
                case NodeKind.ObjectField:
                    writer.WriteObjectField((ObjectFieldNode)node);
                    break;
                case NodeKind.SchemaDefinition:
                    VisitSchemaDefinition((SchemaDefinitionNode)node, writer);
                    break;
                case NodeKind.OperationTypeDefinition:
                    VisitOperationTypeDefinition((OperationTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.ScalarTypeDefinition:
                    VisitScalarTypeDefinition((ScalarTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.ObjectTypeDefinition:
                    VisitObjectTypeDefinition((ObjectTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.FieldDefinition:
                    VisitFieldDefinition((FieldDefinitionNode)node, writer);
                    break;
                case NodeKind.InputValueDefinition:
                    VisitInputValueDefinition((InputValueDefinitionNode)node, writer);
                    break;
                case NodeKind.InterfaceTypeDefinition:
                    VisitInterfaceTypeDefinition((InterfaceTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.UnionTypeDefinition:
                    VisitUnionTypeDefinition((UnionTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.EnumTypeDefinition:
                    VisitEnumTypeDefinition((EnumTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.EnumValueDefinition:
                    VisitEnumValueDefinition((EnumValueDefinitionNode)node, writer);
                    break;
                case NodeKind.InputObjectTypeDefinition:
                    VisitInputObjectTypeDefinition((InputObjectTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.DirectiveDefinition:
                    VisitDirectiveDefinition((DirectiveDefinitionNode)node, writer);
                    break;
                case NodeKind.SchemaExtension:
                    VisitSchemaExtension((SchemaExtensionNode)node, writer);
                    break;
                case NodeKind.ScalarTypeExtension:
                    VisitScalarTypeExtension((ScalarTypeExtensionNode)node, writer);
                    break;
                case NodeKind.ObjectTypeExtension:
                    VisitObjectTypeExtension((ObjectTypeExtensionNode)node, writer);
                    break;
                case NodeKind.InterfaceTypeExtension:
                    VisitInterfaceTypeExtension((InterfaceTypeExtensionNode)node, writer);
                    break;
                case NodeKind.UnionTypeExtension:
                    VisitUnionTypeExtension((UnionTypeExtensionNode)node, writer);
                    break;
                case NodeKind.EnumTypeExtension:
                    VisitEnumTypeExtension((EnumTypeExtensionNode)node, writer);
                    break;
                case NodeKind.InputObjectTypeExtension:
                    VisitInputObjectTypeExtension((InputObjectTypeExtensionNode)node, writer);
                    break;
                default:
                    throw new NotSupportedException(node.GetType().FullName);
            }
        }

        private void VisitDocument(DocumentNode node, ISyntaxWriter writer)
        {
            if (node.Definitions.Count > 0)
            {
                VisitDefinition(node.Definitions[0], writer);

                for (int i = 1; i < node.Definitions.Count; i++)
                {
                    if (_indented)
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteSpace();
                    }

                    VisitDefinition(node.Definitions[i], writer);
                }
            }
        }

        private void VisitDefinition(IDefinitionNode node, ISyntaxWriter writer)
        {
            switch (node.Kind)
            {
                case NodeKind.OperationDefinition:
                    VisitOperationDefinition((OperationDefinitionNode)node, writer);
                    break;
                case NodeKind.FragmentDefinition:
                    VisitFragmentDefinition((FragmentDefinitionNode)node, writer);
                    break;
                case NodeKind.SchemaDefinition:
                    VisitSchemaDefinition((SchemaDefinitionNode)node, writer);
                    break;
                case NodeKind.DirectiveDefinition:
                    VisitDirectiveDefinition((DirectiveDefinitionNode)node, writer);
                    break;
                case NodeKind.ScalarTypeDefinition:
                    VisitScalarTypeDefinition((ScalarTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.ObjectTypeDefinition:
                    VisitObjectTypeDefinition((ObjectTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.InputObjectTypeDefinition:
                    VisitInputObjectTypeDefinition((InputObjectTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.InterfaceTypeDefinition:
                    VisitInterfaceTypeDefinition((InterfaceTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.UnionTypeDefinition:
                    VisitUnionTypeDefinition((UnionTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.EnumTypeDefinition:
                    VisitEnumTypeDefinition((EnumTypeDefinitionNode)node, writer);
                    break;
                case NodeKind.SchemaExtension:
                    VisitSchemaExtension((SchemaExtensionNode)node, writer);
                    break;
                case NodeKind.ScalarTypeExtension:
                    VisitScalarTypeExtension((ScalarTypeExtensionNode)node, writer);
                    break;
                case NodeKind.ObjectTypeExtension:
                    VisitObjectTypeExtension((ObjectTypeExtensionNode)node, writer);
                    break;
                case NodeKind.InterfaceTypeExtension:
                    VisitInterfaceTypeExtension((InterfaceTypeExtensionNode)node, writer);
                    break;
                case NodeKind.UnionTypeExtension:
                    VisitUnionTypeExtension((UnionTypeExtensionNode)node, writer);
                    break;
                case NodeKind.EnumTypeExtension:
                    VisitEnumTypeExtension((EnumTypeExtensionNode)node, writer);
                    break;
                case NodeKind.InputObjectTypeExtension:
                    VisitInputObjectTypeExtension((InputObjectTypeExtensionNode)node, writer);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
