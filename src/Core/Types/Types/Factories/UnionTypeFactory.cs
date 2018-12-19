using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class UnionTypeFactory
        : ITypeFactory<UnionTypeDefinitionNode, UnionType>
    {
        public UnionType Create(UnionTypeDefinitionNode node)
        {
            return new UnionType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                foreach (NamedTypeNode namedType in node.Types)
                {
                    d.Type(namedType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }
            });
        }
    }
}
