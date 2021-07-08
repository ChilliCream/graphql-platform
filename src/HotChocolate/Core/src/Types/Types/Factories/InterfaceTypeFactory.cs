using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories
{
    internal sealed class InterfaceTypeFactory
        : ITypeFactory<InterfaceTypeDefinitionNode, InterfaceType>
        , ITypeFactory<InterfaceTypeExtensionNode, InterfaceTypeExtension>
    {
        public InterfaceType Create(IDescriptorContext context, InterfaceTypeDefinitionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new InterfaceTypeDefinition(
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

            return InterfaceType.CreateUnsafe(typeDefinition);
        }

        public InterfaceTypeExtension Create(IDescriptorContext context, InterfaceTypeExtensionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new InterfaceTypeDefinition(node.Name.Value);

            foreach (NamedTypeNode typeNode in node.Interfaces)
            {
                typeDefinition.Interfaces.Add(TypeReference.Create(typeNode));
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareFields(typeDefinition, node.Fields, preserveSyntaxNodes);

            return InterfaceTypeExtension.CreateUnsafe(typeDefinition);
        }

        private static void DeclareFields(
            InterfaceTypeDefinition parent,
            IReadOnlyCollection<FieldDefinitionNode> fields,
            bool preserveSyntaxNodes)
        {
            foreach (FieldDefinitionNode field in fields)
            {
                var fieldDefinition = new InterfaceFieldDefinition(
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
            InterfaceFieldDefinition parent,
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
