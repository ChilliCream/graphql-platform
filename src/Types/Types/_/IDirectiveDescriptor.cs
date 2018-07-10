using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IDirectiveDescriptor
        : IFluent
    {
        IDirectiveDescriptor SyntaxNode(DirectiveDefinitionNode syntaxNode);

        IDirectiveDescriptor Name(string name);

        IDirectiveDescriptor Description(string description);

        IArgumentDescriptor Argument(string name);

        IDirectiveDescriptor Location(DirectiveLocation location);
    }
}
