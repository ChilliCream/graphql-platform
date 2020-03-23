using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class SchemaSyntaxRewriter<TContext>
        : SyntaxRewriter<TContext>
    {
        public virtual ISyntaxNode Rewrite(ISyntaxNode node, TContext context)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            switch (node)
            {
                case DocumentNode document:
                    return RewriteDocument(document, context);

                case ITypeSystemExtensionNode extension:
                    return RewriteTypeExtensionDefinition(extension, context);

                case ITypeSystemDefinitionNode definition:
                    return RewriteTypeDefinition(definition, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual DocumentNode RewriteDocument(
            DocumentNode node,
            TContext context)
        {
            IReadOnlyList<IDefinitionNode> rewrittenDefinitions =
                RewriteMany(node.Definitions, context, (n, c) =>
                {
                    if (n is ITypeSystemExtensionNode extension)
                    {
                        return RewriteTypeExtensionDefinition(extension, c);
                    }

                    if (n is ITypeSystemDefinitionNode definition)
                    {
                        return RewriteTypeDefinition(definition, c);
                    }

                    throw new NotSupportedException();
                });

            return ReferenceEquals(node.Definitions, rewrittenDefinitions)
                ? node : node.WithDefinitions(rewrittenDefinitions);
        }

        protected virtual ITypeSystemDefinitionNode RewriteTypeDefinition(
            ITypeSystemDefinitionNode node,
            TContext context)
        {
            switch (node)
            {
                case SchemaDefinitionNode value:
                    return RewriteSchemaDefinition(value, context);

                case DirectiveDefinitionNode value:
                    return RewriteDirectiveDefinition(value, context);

                case ScalarTypeDefinitionNode value:
                    return RewriteScalarTypeDefinition(value, context);

                case ObjectTypeDefinitionNode value:
                    return RewriteObjectTypeDefinition(value, context);

                case InputObjectTypeDefinitionNode value:
                    return RewriteInputObjectTypeDefinition(value, context);

                case InterfaceTypeDefinitionNode value:
                    return RewriteInterfaceTypeDefinition(value, context);

                case UnionTypeDefinitionNode value:
                    return RewriteUnionTypeDefinition(value, context);

                case EnumTypeDefinitionNode value:
                    return RewriteEnumTypeDefinition(value, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual ITypeSystemExtensionNode RewriteTypeExtensionDefinition(
            ITypeSystemExtensionNode node,
            TContext context)
        {
            switch (node)
            {
                case SchemaExtensionNode value:
                    return RewriteSchemaExtension(value, context);

                case ScalarTypeExtensionNode value:
                    return RewriteScalarTypeExtension(value, context);

                case ObjectTypeExtensionNode value:
                    return RewriteObjectTypeExtension(value, context);

                case InterfaceTypeExtensionNode value:
                    return RewriteInterfaceTypeExtension(value, context);

                case UnionTypeExtensionNode value:
                    return RewriteUnionTypeExtension(value, context);

                case EnumTypeExtensionNode value:
                    return RewriteEnumTypeExtension(value, context);

                case InputObjectTypeExtensionNode value:
                    return RewriteInputObjectTypeExtension(value, context);

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual SchemaDefinitionNode RewriteSchemaDefinition(
            SchemaDefinitionNode node,
            TContext context)
        {
            SchemaDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = RewriteMany(current, current.OperationTypes, context,
                RewriteOperationTypeDefinition, current.WithOperationTypes);

            return current;
        }

        protected virtual SchemaExtensionNode RewriteSchemaExtension(
            SchemaExtensionNode node,
            TContext context)
        {
            SchemaExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = RewriteMany(current, current.OperationTypes, context,
                RewriteOperationTypeDefinition, current.WithOperationTypes);

            return current;
        }

        protected virtual OperationTypeDefinitionNode
            RewriteOperationTypeDefinition(
            OperationTypeDefinitionNode node,
            TContext context)
        {
            OperationTypeDefinitionNode current = node;

            current = Rewrite(current, current.Type, context,
                RewriteNamedType, current.WithType);

            return current;
        }

        protected virtual DirectiveDefinitionNode RewriteDirectiveDefinition(
            DirectiveDefinitionNode node,
            TContext context)
        {
            DirectiveDefinitionNode current = node;

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Arguments, context,
                RewriteInputValueDefinition, current.WithArguments);

            current = RewriteMany(current, current.Locations, context,
                RewriteName, current.WithLocations);

            return current;
        }

        protected virtual ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
            ScalarTypeDefinitionNode node,
            TContext context)
        {
            ScalarTypeDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            return current;
        }

        protected virtual ScalarTypeExtensionNode RewriteScalarTypeExtension(
            ScalarTypeExtensionNode node,
            TContext context)
        {
            ScalarTypeExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            return current;
        }

        protected virtual ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            TContext context)
        {
            ObjectTypeDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Interfaces, context,
                RewriteNamedType, current.WithInterfaces);

            current = RewriteMany(current, current.Fields, context,
                RewriteFieldDefinition, current.WithFields);

            return current;
        }

        protected virtual ObjectTypeExtensionNode RewriteObjectTypeExtension(
            ObjectTypeExtensionNode node,
            TContext context)
        {
            ObjectTypeExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = RewriteMany(current, current.Interfaces, context,
                RewriteNamedType, current.WithInterfaces);

            current = RewriteMany(current, current.Fields, context,
                RewriteFieldDefinition, current.WithFields);

            current = RewriteMany(current, current.Fields, context,
                RewriteFieldDefinition, current.WithFields);

            return current;
        }

        protected virtual FieldDefinitionNode RewriteFieldDefinition(
            FieldDefinitionNode node,
            TContext context)
        {
            FieldDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Arguments, context,
                RewriteInputValueDefinition, current.WithArguments);

            current = Rewrite(current, current.Type, context,
                RewriteType, current.WithType);

            return current;
        }

        protected virtual InputObjectTypeDefinitionNode
            RewriteInputObjectTypeDefinition(
                InputObjectTypeDefinitionNode node,
                TContext context)
        {
            InputObjectTypeDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Fields, context,
                RewriteInputValueDefinition, current.WithFields);

            return current;
        }

        protected virtual InputObjectTypeExtensionNode
            RewriteInputObjectTypeExtension(
                InputObjectTypeExtensionNode node,
                TContext context)
        {
            InputObjectTypeExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = RewriteMany(current, current.Fields, context,
                RewriteInputValueDefinition, current.WithFields);

            return current;
        }

        protected virtual InputValueDefinitionNode RewriteInputValueDefinition(
            InputValueDefinitionNode node,
            TContext context)
        {
            InputValueDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = Rewrite(current, current.Type, context,
                RewriteType, current.WithType);

            current = Rewrite(current, current.DefaultValue, context,
                RewriteValue, current.WithDefaultValue);

            return current;
        }

        protected virtual InterfaceTypeDefinitionNode
            RewriteInterfaceTypeDefinition(
                InterfaceTypeDefinitionNode node,
                TContext context)
        {
            InterfaceTypeDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Fields, context,
                RewriteFieldDefinition, current.WithFields);

            return current;
        }

        protected virtual InterfaceTypeExtensionNode
            RewriteInterfaceTypeExtension(
                InterfaceTypeExtensionNode node,
                TContext context)
        {
            InterfaceTypeExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = RewriteMany(current, current.Fields, context,
                RewriteFieldDefinition, current.WithFields);

            return current;
        }

        protected virtual UnionTypeDefinitionNode RewriteUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            TContext context)
        {
            UnionTypeDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Types, context,
                RewriteNamedType, current.WithTypes);

            return current;
        }

        protected virtual UnionTypeExtensionNode RewriteUnionTypeExtension(
            UnionTypeExtensionNode node,
            TContext context)
        {
            UnionTypeExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = RewriteMany(current, current.Types, context,
                RewriteNamedType, current.WithTypes);

            return current;
        }

        protected virtual EnumTypeDefinitionNode RewriteEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            TContext context)
        {
            EnumTypeDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            current = RewriteMany(current, current.Values, context,
                RewriteEnumValueDefinition, current.WithValues);

            return current;
        }

        protected virtual EnumTypeExtensionNode RewriteEnumTypeExtension(
            EnumTypeExtensionNode node,
            TContext context)
        {
            EnumTypeExtensionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = RewriteMany(current, current.Values, context,
                RewriteEnumValueDefinition, current.WithValues);

            return current;
        }

        protected virtual EnumValueDefinitionNode RewriteEnumValueDefinition(
           EnumValueDefinitionNode node,
           TContext context)
        {
            EnumValueDefinitionNode current = node;

            current = RewriteDirectives(current, current.Directives,
                context, current.WithDirectives);

            current = Rewrite(current, current.Name, context,
                RewriteName, current.WithName);

            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);

            return current;
        }
    }
}
