using System;

namespace HotChocolate.Language
{
    public class SchemaSyntaxWalker<TContext>
        : SyntaxWalkerBase<DocumentNode, TContext>
    {
        protected SchemaSyntaxWalker()
        {
        }

        public override void Visit(
            DocumentNode node,
            TContext context)
        {
            if (node != null)
            {
                VisitDocument(node, context);
            }
        }

        protected override void VisitDocument(
            DocumentNode node,
            TContext context)
        {
            VisitMany(node.Definitions, context, VisitDefinition);
        }

        protected virtual void VisitDefinition(
            IDefinitionNode node,
            TContext context)
        {
            if (node is ITypeSystemExtensionNode)
            {
                VisitTypeExtensionDefinition(node, context);
            }
            else
            {
                VisitTypeDefinition(node, context);
            }
        }


        protected virtual void VisitTypeDefinition(
            IDefinitionNode node,
            TContext context)
        {
            switch (node)
            {
                case SchemaDefinitionNode value:
                    VisitSchemaDefinition(value, context);
                    break;
                case DirectiveDefinitionNode value:
                    VisitDirectiveDefinition(value, context);
                    break;
                case ScalarTypeDefinitionNode value:
                    VisitScalarTypeDefinition(value, context);
                    break;
                case ObjectTypeDefinitionNode value:
                    VisitObjectTypeDefinition(value, context);
                    break;
                case InputObjectTypeDefinitionNode value:
                    VisitInputObjectTypeDefinition(value, context);
                    break;
                case InterfaceTypeDefinitionNode value:
                    VisitInterfaceTypeDefinition(value, context);
                    break;
                case UnionTypeDefinitionNode value:
                    VisitUnionTypeDefinition(value, context);
                    break;
                case EnumTypeDefinitionNode value:
                    VisitEnumTypeDefinition(value, context);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual void VisitTypeExtensionDefinition(
            IDefinitionNode node,
            TContext context)
        {
            switch (node)
            {
                case SchemaExtensionNode value:
                    VisitSchemaExtension(value, context);
                    break;
                case ScalarTypeExtensionNode value:
                    VisitScalarTypeExtension(value, context);
                    break;
                case ObjectTypeExtensionNode value:
                    VisitObjectTypeExtension(value, context);
                    break;
                case InterfaceTypeExtensionNode value:
                    VisitInterfaceTypeExtension(value, context);
                    break;
                case UnionTypeExtensionNode value:
                    VisitUnionTypeExtension(value, context);
                    break;
                case EnumTypeExtensionNode value:
                    VisitEnumTypeExtension(value, context);
                    break;
                case InputObjectTypeExtensionNode value:
                    VisitInputObjectTypeExtension(value, context);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node,
            TContext context)
        {
            VisitMany(
                node.Directives,
                context,
                VisitDirective);

            VisitMany(
                node.OperationTypes,
                context,
                VisitOperationTypeDefinition);
        }

        protected override void VisitSchemaExtension(
            SchemaExtensionNode node,
            TContext context)
        {
            VisitMany(
                node.Directives,
                context,
                VisitDirective);

            VisitMany(
                node.OperationTypes,
                context,
                VisitOperationTypeDefinition);
        }

        protected override void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node,
            TContext context)
        {
            VisitNamedType(node.Type, context);
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Arguments, context, VisitInputValueDefinition);
            VisitMany(node.Locations, context, VisitName);
        }

        protected override void VisitScalarTypeDefinition(
            ScalarTypeDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Directives, context, VisitDirective);
        }

        protected override void VisitScalarTypeExtension(
            ScalarTypeExtensionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Interfaces, context, VisitNamedType);
            VisitMany(node.Fields, context, VisitFieldDefinition);
        }

        protected override void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Interfaces, context, VisitNamedType);
            VisitMany(node.Fields, context, VisitFieldDefinition);
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Arguments, context, VisitInputValueDefinition);
            VisitType(node.Type, context);
            VisitMany(node.Directives, context, VisitDirective);
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Fields, context, VisitInputValueDefinition);
        }

        protected override void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Fields, context, VisitInputValueDefinition);
        }

        protected override void VisitInterfaceTypeDefinition(
           InterfaceTypeDefinitionNode node,
           TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Fields, context, VisitFieldDefinition);
        }

        protected override void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Fields, context, VisitFieldDefinition);
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Types, context, VisitNamedType);
        }

        protected override void VisitUnionTypeExtension(
            UnionTypeExtensionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Types, context, VisitNamedType);
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitIfNotNull(node.Description, context, VisitStringValue);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Values, context, VisitEnumValueDefinition);
        }

        protected override void VisitEnumTypeExtension(
            EnumTypeExtensionNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Directives, context, VisitDirective);
            VisitMany(node.Values, context, VisitEnumValueDefinition);
        }

        private static void VisitIfNotNull<T>(
            T node,
            TContext context,
            Action<T, TContext> visitor)
            where T : class
        {
            if (node != null)
            {
                visitor(node, context);
            }
        }
    }
}
