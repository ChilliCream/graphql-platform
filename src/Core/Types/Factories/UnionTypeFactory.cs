using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class UnionTypeFactory
        : ITypeFactory<UnionTypeDefinitionNode, UnionType>
    {
        public UnionType Create(
            SchemaContext context,
            UnionTypeDefinitionNode node)
        {
            return new UnionType(new UnionTypeConfig
            {
                SyntaxNode = node,
                Name = node.Name.Value,
                Description = node.Description?.Value,
                TypeResolver = context.CreateTypeResolver(node.Name.Value),
                Types = () => node.Types.Select(t => context.GetOutputType<ObjectType>(t.Name.Value))
            });
        }
    }
}
