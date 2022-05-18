using System;
using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters;

public class DefaultSyntaxNavigator : ISyntaxNavigator
{
    private readonly List<ISyntaxNode> _ancestors = new();

    public void Push(ISyntaxNode node)
    {
        _ancestors.Add(node);
    }

    public void Pop(out ISyntaxNode node)
    {
        node = _ancestors[_ancestors.Count - 1];
        _ancestors.RemoveAt(_ancestors.Count - 1);
    }

    public TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is not TNode typedNode)
            {
                continue;
            }

            return typedNode;
        }

        return default;
    }

    public IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is not TNode typedNode)
            {
                continue;
            }

            yield return typedNode;
        }
    }

    public ISyntaxNode? GetParent()
    {
        return GetAncestor<ISyntaxNode>();
    }
}
