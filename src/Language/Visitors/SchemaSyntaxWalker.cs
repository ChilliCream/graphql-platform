namespace HotChocolate.Language
{
    public class SchemaSyntaxWalker
        : SyntaxVisitor<DocumentNode>
    {
        public override void Visit(DocumentNode node)
        {
            if (node != null)
            {
                VisitDocument(node);
            }
        }

        protected override void VisitDocument(DocumentNode node)
        {
            VisitMany(node.Definitions, VisitDefinition);
        }

        protected virtual void VisitDefinition(IDefinitionNode node)
        {
            switch (node)
            {
                case SchemaDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case ScalarTypeDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case ObjectTypeDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case InputObjectTypeDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case InterfaceTypeDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case UnionTypeDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case EnumTypeDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case ScalarTypeExtensionNode value:
                    VisitOperationDefinition(value);
                    break;
                case ObjectTypeExtensionNode value:
                    VisitOperationDefinition(value);
                    break;
                case InterfaceTypeExtensionNode value:
                    VisitOperationDefinition(value);
                    break;
                case UnionTypeExtensionNode value:
                    VisitOperationDefinition(value);
                    break;
                case EnumTypeExtensionNode value:
                    VisitOperationDefinition(value);
                    break;
                case InputObjectTypeExtensionNode value:
                    VisitOperationDefinition(value);
                    break;
                case DirectiveDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
            }
        }







        protected virtual void VisitUnsupportedDefinitions(
            IDefinitionNode node)
        {
        }




        protected override void VisitListValue(ListValueNode node)
        {
            VisitMany(node.Items, VisitValue);
        }

        protected override void VisitObjectValue(ObjectValueNode node)
        {
            VisitMany(node.Fields, VisitObjectField);
        }

        protected override void VisitObjectField(ObjectFieldNode node)
        {
            VisitName(node.Name);
            VisitValue(node.Value);
        }

        protected override void VisitVariable(VariableNode node)
        {
            VisitName(node.Name);
        }

        protected override void VisitDirective(DirectiveNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Arguments, VisitArgument);
        }

        protected override void VisitArgument(ArgumentNode node)
        {
            VisitName(node.Name);
            VisitValue(node.Value);
        }

        protected override void VisitListType(ListTypeNode node)
        {
            VisitType(node.Type);
        }

        protected override void VisitNonNullType(NonNullTypeNode node)
        {
            VisitType(node.Type);
        }

        protected override void VisitNamedType(NamedTypeNode node)
        {
            VisitName(node.Name);
        }
    }
}
