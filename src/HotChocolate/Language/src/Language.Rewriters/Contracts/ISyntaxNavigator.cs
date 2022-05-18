using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters;

public interface ISyntaxNavigator
{
    void Push(ISyntaxNode node);

    void Pop(out ISyntaxNode node);

    TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode;

    IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode;

    ISyntaxNode? GetParent();
}
