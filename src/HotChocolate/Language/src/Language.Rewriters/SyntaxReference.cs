using System;
using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters;

public sealed class SyntaxReference
{
    private readonly IReadOnlyList<ISyntaxNode> _ancestors;

    public SyntaxReference(IReadOnlyList<ISyntaxNode> ancestors, ISyntaxNode node)
    {
        _ancestors = ancestors;
        if (_ancestors.Count == 0)
        {
            throw new InvalidOperationException();
        }

        Node = node;
    }

    public ISyntaxNode Node { get; }

    public T? GetAncestor<T>()
        where T : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            ISyntaxNode current = _ancestors[i];
            if (current is not T typedReference)
            {
                continue;
            }

            return typedReference;
        }

        return default;
    }

    public IEnumerable<T> GetAncestors<T>()
        where T : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            ISyntaxNode current = _ancestors[i];
            if (current is not T typedReference)
            {
                continue;
            }

            yield return typedReference;
        }
    }

    public ISyntaxNode? GetParent()
    {
        if (_ancestors.Count > 0)
        {
            return _ancestors[_ancestors.Count - 1];
        }

        return default;
    }
}
