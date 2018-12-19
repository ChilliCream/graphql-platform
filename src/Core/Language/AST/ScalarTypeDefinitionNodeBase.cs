using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class ScalarTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected ScalarTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives)
        { }
    }
}
