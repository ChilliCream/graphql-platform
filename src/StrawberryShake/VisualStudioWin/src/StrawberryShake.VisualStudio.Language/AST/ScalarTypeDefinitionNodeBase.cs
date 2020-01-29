using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public abstract class ScalarTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected ScalarTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        { }
    }
}
