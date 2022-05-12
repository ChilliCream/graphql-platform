using System;
using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters.Contracts;

public interface ISyntaxNavigator
{
    IDisposable Push(ISyntaxNode node);

    TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode;

    IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode;

    SchemaCoordinate CreateCoordinate();
}
