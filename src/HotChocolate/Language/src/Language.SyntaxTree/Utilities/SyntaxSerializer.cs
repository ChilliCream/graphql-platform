using System;

namespace HotChocolate.Language.Utilities
{
    public sealed partial class SyntaxSerializer
    {
        private bool _indent;

        public void Serialize(ISyntaxNode node, ISyntaxWriter writer)
        {

        }

        private void VisitDocument(DocumentNode node, ISyntaxWriter writer)
        {
            if (node.Definitions.Count > 0)
            {
                VisitDefinition(node.Definitions[0], writer);

                for (int i = 1; i < node.Definitions.Count; i++)
                {
                    if (_indent)
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
