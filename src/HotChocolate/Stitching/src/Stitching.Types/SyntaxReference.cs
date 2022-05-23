using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public sealed class SyntaxReference
{
    private readonly IReadOnlyList<ISyntaxNode> _ancestors;

    public SyntaxReference(IReadOnlyList<ISyntaxNode> ancestors, ISyntaxNode? node)
    {
        if (ancestors.Count == 0)
        {
            throw new InvalidOperationException();
        }

        _ancestors = new List<ISyntaxNode>(ancestors);

        Node = node;
    }

    public ISyntaxNode? Node { get; }

    public T? GetAncestor<T>()
        where T : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            ISyntaxNode current = _ancestors[i];
            if (current is T typedReference)
            {
                return typedReference;
            }
        }

        return default;
    }

    public IEnumerable<T> GetAncestors<T>()
        where T : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            ISyntaxNode current = _ancestors[i];
            if (current is T typedReference)
            {
                yield return typedReference;
            }
        }
    }

    public ISyntaxNode? Parent
    {
        get
        {
            if (_ancestors.Count > 0)
            {
                return _ancestors[_ancestors.Count - 1];
            }

            return default;
        }
    }
}
