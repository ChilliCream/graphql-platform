using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface ISelectionNode
        : ISyntaxNode
    {
        IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}
