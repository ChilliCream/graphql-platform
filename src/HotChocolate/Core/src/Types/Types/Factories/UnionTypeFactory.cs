using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories
{
    internal sealed class UnionTypeFactory : ITypeFactory<UnionTypeDefinitionNode, UnionType>
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
    }
}
