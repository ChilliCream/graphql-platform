namespace HotChocolate.Language.Utilities;

/// <summary>
/// This helper can serialize a GraphQL syntax tree into its string representation.
/// </summary>
public sealed partial class SyntaxSerializer
{
    private readonly bool _indented;
    private readonly int _maxDirectivesPerLine;

    public SyntaxSerializer(SyntaxSerializerOptions options = default)
    {
        _indented = options.Indented;
        _maxDirectivesPerLine = options.MaxDirectivesPerLine;
    }

    public void Serialize(ISyntaxNode node, ISyntaxWriter writer)
    {
        switch (node.Kind)
        {
            case SyntaxKind.Name:
                writer.WriteName((NameNode)node);
                break;
            case SyntaxKind.Document:
                VisitDocument((DocumentNode)node, writer);
                break;
            case SyntaxKind.OperationDefinition:
                VisitOperationDefinition((OperationDefinitionNode)node, writer);
                break;
            case SyntaxKind.VariableDefinition:
                VisitVariableDefinition((VariableDefinitionNode)node, writer);
                break;
            case SyntaxKind.Variable:
                writer.WriteVariable((VariableNode)node);
                break;
            case SyntaxKind.SelectionSet:
                VisitSelectionSet((SelectionSetNode)node, writer);
                break;
            case SyntaxKind.Field:
                VisitField((FieldNode)node, writer);
                break;
            case SyntaxKind.Argument:
                writer.WriteArgument((ArgumentNode)node);
                break;
            case SyntaxKind.FragmentSpread:
                VisitFragmentSpread((FragmentSpreadNode)node, writer);
                break;
            case SyntaxKind.InlineFragment:
                VisitInlineFragment((InlineFragmentNode)node, writer);
                break;
            case SyntaxKind.FragmentDefinition:
                VisitFragmentDefinition((FragmentDefinitionNode)node, writer);
                break;
            case SyntaxKind.Directive:
                writer.WriteDirective((DirectiveNode)node);
                break;
            case SyntaxKind.NamedType:
            case SyntaxKind.ListType:
            case SyntaxKind.NonNullType:
                writer.WriteType((ITypeNode)node);
                break;
            case SyntaxKind.ListValue:
            case SyntaxKind.ObjectValue:
            case SyntaxKind.BooleanValue:
            case SyntaxKind.EnumValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.NullValue:
            case SyntaxKind.StringValue:
                writer.WriteValue((IValueNode)node);
                break;
            case SyntaxKind.ObjectField:
                writer.WriteObjectField((ObjectFieldNode)node);
                break;
            case SyntaxKind.SchemaDefinition:
                VisitSchemaDefinition((SchemaDefinitionNode)node, writer);
                break;
            case SyntaxKind.OperationTypeDefinition:
                VisitOperationTypeDefinition((OperationTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.ScalarTypeDefinition:
                VisitScalarTypeDefinition((ScalarTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.ObjectTypeDefinition:
                VisitObjectTypeDefinition((ObjectTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.FieldDefinition:
                VisitFieldDefinition((FieldDefinitionNode)node, writer);
                break;
            case SyntaxKind.InputValueDefinition:
                VisitInputValueDefinition((InputValueDefinitionNode)node, writer);
                break;
            case SyntaxKind.InterfaceTypeDefinition:
                VisitInterfaceTypeDefinition((InterfaceTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.UnionTypeDefinition:
                VisitUnionTypeDefinition((UnionTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.EnumTypeDefinition:
                VisitEnumTypeDefinition((EnumTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.EnumValueDefinition:
                VisitEnumValueDefinition((EnumValueDefinitionNode)node, writer);
                break;
            case SyntaxKind.InputObjectTypeDefinition:
                VisitInputObjectTypeDefinition((InputObjectTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.DirectiveDefinition:
                VisitDirectiveDefinition((DirectiveDefinitionNode)node, writer);
                break;
            case SyntaxKind.SchemaExtension:
                VisitSchemaExtension((SchemaExtensionNode)node, writer);
                break;
            case SyntaxKind.ScalarTypeExtension:
                VisitScalarTypeExtension((ScalarTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.ObjectTypeExtension:
                VisitObjectTypeExtension((ObjectTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.InterfaceTypeExtension:
                VisitInterfaceTypeExtension((InterfaceTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.UnionTypeExtension:
                VisitUnionTypeExtension((UnionTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.EnumTypeExtension:
                VisitEnumTypeExtension((EnumTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.InputObjectTypeExtension:
                VisitInputObjectTypeExtension((InputObjectTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.SchemaCoordinate:
                VisitSchemaCoordinate((SchemaCoordinateNode)node, writer);
                break;
            default:
                ThrowHelper.NodeKindIsNotSupported(node.Kind);
                break;
        }
    }

    private void VisitDocument(DocumentNode node, ISyntaxWriter writer)
    {
        if (node.Definitions.Count > 0)
        {
            VisitDefinition(node.Definitions[0], writer);

            for (var i = 1; i < node.Definitions.Count; i++)
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
            case SyntaxKind.OperationDefinition:
                VisitOperationDefinition((OperationDefinitionNode)node, writer);
                break;
            case SyntaxKind.FragmentDefinition:
                VisitFragmentDefinition((FragmentDefinitionNode)node, writer);
                break;
            case SyntaxKind.SchemaDefinition:
                VisitSchemaDefinition((SchemaDefinitionNode)node, writer);
                break;
            case SyntaxKind.DirectiveDefinition:
                VisitDirectiveDefinition((DirectiveDefinitionNode)node, writer);
                break;
            case SyntaxKind.ScalarTypeDefinition:
                VisitScalarTypeDefinition((ScalarTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.ObjectTypeDefinition:
                VisitObjectTypeDefinition((ObjectTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.InputObjectTypeDefinition:
                VisitInputObjectTypeDefinition((InputObjectTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.InterfaceTypeDefinition:
                VisitInterfaceTypeDefinition((InterfaceTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.UnionTypeDefinition:
                VisitUnionTypeDefinition((UnionTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.EnumTypeDefinition:
                VisitEnumTypeDefinition((EnumTypeDefinitionNode)node, writer);
                break;
            case SyntaxKind.SchemaExtension:
                VisitSchemaExtension((SchemaExtensionNode)node, writer);
                break;
            case SyntaxKind.ScalarTypeExtension:
                VisitScalarTypeExtension((ScalarTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.ObjectTypeExtension:
                VisitObjectTypeExtension((ObjectTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.InterfaceTypeExtension:
                VisitInterfaceTypeExtension((InterfaceTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.UnionTypeExtension:
                VisitUnionTypeExtension((UnionTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.EnumTypeExtension:
                VisitEnumTypeExtension((EnumTypeExtensionNode)node, writer);
                break;
            case SyntaxKind.InputObjectTypeExtension:
                VisitInputObjectTypeExtension((InputObjectTypeExtensionNode)node, writer);
                break;
            default:
                ThrowHelper.NodeKindIsNotSupported(node.Kind);
                break;
        }
    }
}
