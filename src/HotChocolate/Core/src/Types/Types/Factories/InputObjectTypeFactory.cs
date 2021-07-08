using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories
{
    internal sealed class InputObjectTypeFactory
        : ITypeFactory<InputObjectTypeDefinitionNode, InputObjectType>
        , ITypeFactory<InputObjectTypeExtensionNode, InputObjectTypeExtension>
    {
        public InputObjectType Create(
            IDescriptorContext context,
            InputObjectTypeDefinitionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new InputObjectTypeDefinition(
                node.Name.Value,
                node.Description?.Value);

            if (preserveSyntaxNodes)
            {
                typeDefinition.SyntaxNode = node;
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareFields(typeDefinition, node.Fields, preserveSyntaxNodes);

            return InputObjectType.CreateUnsafe(typeDefinition);
        }

        public InputObjectTypeExtension Create(IDescriptorContext context, InputObjectTypeExtensionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new InputObjectTypeDefinition(node.Name.Value);

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareFields(typeDefinition, node.Fields, preserveSyntaxNodes);

            return InputObjectTypeExtension.CreateUnsafe(typeDefinition);
        }

        private static void DeclareFields(
            InputObjectTypeDefinition parent,
            IReadOnlyCollection<InputValueDefinitionNode> fields,
            bool preserveSyntaxNodes)
        {
            foreach (InputValueDefinitionNode inputField in fields)
            {
                var inputFieldDefinition = new InputFieldDefinition(
                    inputField.Name.Value,
                    inputField.Description?.Value,
                    TypeReference.Create(inputField.Type),
                    inputField.DefaultValue);

                if (preserveSyntaxNodes)
                {
                    inputFieldDefinition.SyntaxNode = inputField;
                }

                SdlToTypeSystemHelper.AddDirectives(inputFieldDefinition, inputField);

                parent.Fields.Add(inputFieldDefinition);
            }
        }
    }
}
