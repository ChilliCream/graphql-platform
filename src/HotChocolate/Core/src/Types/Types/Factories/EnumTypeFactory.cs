using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories
{
    internal sealed class EnumTypeFactory
        : ITypeFactory<EnumTypeDefinitionNode, EnumType>
        , ITypeFactory<EnumTypeExtensionNode, EnumTypeExtension>
    {

        public EnumType Create(IDescriptorContext context, EnumTypeDefinitionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new EnumTypeDefinition(
                node.Name.Value,
                node.Description?.Value);

            if (preserveSyntaxNodes)
            {
                typeDefinition.SyntaxNode = node;
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareValues(typeDefinition, node.Values, preserveSyntaxNodes);

            return EnumType.CreateUnsafe(typeDefinition);
        }

        public EnumTypeExtension Create(IDescriptorContext context, EnumTypeExtensionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new EnumTypeDefinition(node.Name.Value);

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            DeclareValues(typeDefinition, node.Values, preserveSyntaxNodes);

            return EnumTypeExtension.CreateUnsafe(typeDefinition);
        }

        private static void DeclareValues(
            EnumTypeDefinition parent,
            IReadOnlyCollection<EnumValueDefinitionNode> values,
            bool preserveSyntaxNodes)
        {
            foreach (EnumValueDefinitionNode value in values)
            {
                var valueDefinition = new EnumValueDefinition(
                    value.Name.Value,
                    value.Description?.Value,
                    value.Name.Value);

                if (preserveSyntaxNodes)
                {
                    valueDefinition.SyntaxNode = value;
                }

                SdlToTypeSystemHelper.AddDirectives(valueDefinition, value);

                if (value.DeprecationReason() is { Length: > 0 } reason)
                {
                    valueDefinition.DeprecationReason = reason;
                }

                parent.Values.Add(valueDefinition);
            }
        }
    }
}
