using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class UnionTypeFactory
        : ITypeFactory<UnionTypeDefinitionNode, UnionType>
    {
        public UnionType Create(UnionTypeDefinitionNode node)
        {
            return new UnionType(new UnionTypeConfig
            {
                SyntaxNode = node,
                Name = node.Name.Value,
                Description = node.Description?.Value,
                ResolveAbstractType = context.CreateTypeResolver(node.Name.Value),
                Types = t => node.Types.Select(tn => t.GetOutputType(tn))
            });
        }
    }
}
