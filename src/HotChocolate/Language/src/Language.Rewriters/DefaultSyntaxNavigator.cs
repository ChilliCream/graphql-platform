using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Language.Rewriters.LangRewritersResources;

namespace HotChocolate.Language.Rewriters;

/// <summary>
/// Represents the default implementation of <see cref="ISyntaxNavigator" />
/// </summary>
public class DefaultSyntaxNavigator : ISyntaxNavigator
{
    private readonly List<ISyntaxNode> _ancestors = new();

    /// <inheritdoc cref="ISyntaxNavigator.Push"/>
    public void Push(ISyntaxNode node) => _ancestors.Add(node);

    /// <inheritdoc cref="ISyntaxNavigator.Pop"/>
    public void Pop(out ISyntaxNode node)
    {
        if (!TryPop(out node!))
        {
            throw new InvalidOperationException(DefaultSyntaxNavigator_NoAncestors);
        }
    }

    /// <inheritdoc cref="ISyntaxNavigator.TryPop"/>
    public bool TryPop([NotNullWhen(true)] out ISyntaxNode? node)
    {
        if (_ancestors.Count == 0)
        {
            node = default;
            return false;
        }

        node = _ancestors[_ancestors.Count - 1];
        _ancestors.RemoveAt(_ancestors.Count - 1);
        return true;
    }

    /// <inheritdoc cref="ISyntaxNavigator.GetAncestor{TNode}"/>
    public TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is TNode typedNode)
            {
                return typedNode;
            }
        }

        return default;
    }

    /// <inheritdoc cref="ISyntaxNavigator.GetAncestors{TNode}"/>
    public IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is TNode typedNode)
            {
                yield return typedNode;
            }
        }
    }

    /// <inheritdoc cref="ISyntaxNavigator.GetParent"/>
    public ISyntaxNode? GetParent()
    {
        if(_ancestors.Count == 0)
        {
            return null;
        }

        return _ancestors[_ancestors.Count - 1];
    }
}
