using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories
{
    internal sealed class UnionTypeFactory
        : ITypeFactory<UnionTypeDefinitionNode, UnionType>
        , ITypeFactory<UnionTypeExtensionNode, UnionTypeExtension>
    {
        public UnionType Create(IDescriptorContext context, UnionTypeDefinitionNode node)
        {
            var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;

            var typeDefinition = new UnionTypeDefinition(
                node.Name.Value,
                node.Description?.Value);

            if (preserveSyntaxNodes)
            {
                typeDefinition.SyntaxNode = node;
            }

            foreach (NamedTypeNode namedType in node.Types)
            {
                typeDefinition.Types.Add(TypeReference.Create(namedType));
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            return UnionType.CreateUnsafe(typeDefinition);
        }

        public UnionTypeExtension Create(IDescriptorContext context, UnionTypeExtensionNode node)
        {
            var typeDefinition = new UnionTypeDefinition(node.Name.Value);

            foreach (NamedTypeNode namedType in node.Types)
            {
                typeDefinition.Types.Add(TypeReference.Create(namedType));
            }

            SdlToTypeSystemHelper.AddDirectives(typeDefinition, node);

            return UnionTypeExtension.CreateUnsafe(typeDefinition);
        }
    }
}
