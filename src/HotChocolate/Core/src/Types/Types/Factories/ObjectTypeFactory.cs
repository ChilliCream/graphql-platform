using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Factories
{
    internal sealed class ObjectTypeFactory
        : ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
        , ITypeFactory<ObjectTypeExtensionNode, ObjectTypeExtension>
    {
        public ObjectType Create(IDescriptorContext context, ObjectTypeDefinitionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new ObjectTypeDefinition(
                node.Name.Value,
                node.Description?.Value);

            if (preserveSyntaxNodes)
            {
                typeDefinition.SyntaxNode = node;
            }

            foreach (NamedTypeNode typeNode in node.Interfaces)
            {
                typeDefinition.Interfaces.Add(TypeReference.Create(typeNode));
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareFields(typeDefinition, node.Fields, preserveSyntaxNodes);

            return ObjectType.CreateUnsafe(typeDefinition);
        }

        public ObjectTypeExtension Create(IDescriptorContext context, ObjectTypeExtensionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new ObjectTypeDefinition(node.Name.Value);

            foreach (NamedTypeNode typeNode in node.Interfaces)
            {
                typeDefinition.Interfaces.Add(TypeReference.Create(typeNode));
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareFields(typeDefinition, node.Fields, preserveSyntaxNodes);

            return ObjectTypeExtension.CreateUnsafe(typeDefinition);
        }

        private static void DeclareFields(
            ObjectTypeDefinition parent,
            IReadOnlyCollection<FieldDefinitionNode> fields,
            bool preserveSyntaxNodes)
        {
            foreach (FieldDefinitionNode field in fields)
            {
                var fieldDefinition = new ObjectFieldDefinition(
                    field.Name.Value,
                    field.Description?.Value,
                    TypeReference.Create(field.Type));

                if (preserveSyntaxNodes)
                {
                    fieldDefinition.SyntaxNode = field;
                }

                SdlToTypeSystemHelper.AddDirectives(fieldDefinition, field);

                if (field.DeprecationReason() is { Length: > 0 } reason)
                {
                    fieldDefinition.DeprecationReason = reason;
                }

                DeclareFieldArguments(fieldDefinition, field, preserveSyntaxNodes);

                parent.Fields.Add(fieldDefinition);
            }
        }

        private static void DeclareFieldArguments(
            ObjectFieldDefinition parent,
            FieldDefinitionNode field,
            bool preserveSyntaxNodes)
        {
            foreach (InputValueDefinitionNode argument in field.Arguments)
            {
                var argumentDefinition = new ArgumentDefinition(
                    argument.Name.Value,
                    argument.Description?.Value,
                    TypeReference.Create(argument.Type),
                    argument.DefaultValue);

                if (preserveSyntaxNodes)
                {
                    argumentDefinition.SyntaxNode = argument;
                }

                SdlToTypeSystemHelper.AddDirectives(argumentDefinition, argument);

                parent.Arguments.Add(argumentDefinition);
            }
        }
    }
}
