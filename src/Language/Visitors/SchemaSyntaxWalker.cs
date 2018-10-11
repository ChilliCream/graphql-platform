using System;

namespace HotChocolate.Language
{
    public class SchemaSyntaxWalker
        : SyntaxWalkerBase<DocumentNode>
    {
        protected SchemaSyntaxWalker()
        {
        }

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
            if (node is ITypeExtensionNode)
            {
                VisitTypeExtensionDefinition(node);
            }
            else
            {
                VisitTypeDefinition(node);
            }
        }


        protected virtual void VisitTypeDefinition(IDefinitionNode node)
        {
            switch (node)
            {
                case SchemaDefinitionNode value:
                    VisitSchemaDefinition(value);
                    break;
                case DirectiveDefinitionNode value:
                    VisitDirectiveDefinition(value);
                    break;
                case ScalarTypeDefinitionNode value:
                    VisitScalarTypeDefinition(value);
                    break;
                case ObjectTypeDefinitionNode value:
                    VisitObjectTypeDefinition(value);
                    break;
                case InputObjectTypeDefinitionNode value:
                    VisitInputObjectTypeDefinition(value);
                    break;
                case InterfaceTypeDefinitionNode value:
                    VisitInterfaceTypeDefinition(value);
                    break;
                case UnionTypeDefinitionNode value:
                    VisitUnionTypeDefinition(value);
                    break;
                case EnumTypeDefinitionNode value:
                    VisitEnumTypeDefinition(value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual void VisitTypeExtensionDefinition(IDefinitionNode node)
        {
            switch (node)
            {
                case ScalarTypeExtensionNode value:
                    VisitScalarTypeExtension(value);
                    break;
                case ObjectTypeExtensionNode value:
                    VisitObjectTypeExtension(value);
                    break;
                case InterfaceTypeExtensionNode value:
                    VisitInterfaceTypeExtension(value);
                    break;
                case UnionTypeExtensionNode value:
                    VisitUnionTypeExtension(value);
                    break;
                case EnumTypeExtensionNode value:
                    VisitEnumTypeExtension(value);
                    break;
                case InputObjectTypeExtensionNode value:
                    VisitInputObjectTypeExtension(value);
                    break;
            }
        }

        protected override void VisitSchemaDefinition(SchemaDefinitionNode node)
        {
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.OperationTypes, VisitOperationTypeDefinition);
        }

        protected override void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node)
        {
            VisitNamedType(node.Type);
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Arguments, VisitInputValueDefinition);
            VisitMany(node.Locations, VisitName);
        }

        protected override void VisitScalarTypeDefinition(
            ScalarTypeDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Directives, VisitDirective);
        }

        protected override void VisitScalarTypeExtension(
            ScalarTypeExtensionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Interfaces, VisitNamedType);
            VisitMany(node.Fields, VisitFieldDefinition);
        }

        protected override void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Interfaces, VisitNamedType);
            VisitMany(node.Fields, VisitFieldDefinition);
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Arguments, VisitInputValueDefinition);
            VisitType(node.Type);
            VisitMany(node.Directives, VisitDirective);
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Fields, VisitInputValueDefinition);
        }

        protected override void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Fields, VisitInputValueDefinition);
        }

        protected override void VisitInterfaceTypeDefinition(
           InterfaceTypeDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Fields, VisitFieldDefinition);
        }

        protected override void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Fields, VisitFieldDefinition);
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Types, VisitNamedType);
        }

        protected override void VisitUnionTypeExtension(
            UnionTypeExtensionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Types, VisitNamedType);
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node)
        {
            VisitName(node.Name);
            VisitStringValue(node.Description);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Values, VisitEnumValueDefinition);
        }

        protected override void VisitEnumTypeExtension(
            EnumTypeExtensionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
            VisitMany(node.Values, VisitEnumValueDefinition);
        }
    }
}
