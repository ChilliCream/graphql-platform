using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters.Contracts;

public interface ISyntaxReference
{
    ISyntaxReference? Parent { get; }

    ISyntaxNode Node { get; }

    TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode;

    IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode;
}
