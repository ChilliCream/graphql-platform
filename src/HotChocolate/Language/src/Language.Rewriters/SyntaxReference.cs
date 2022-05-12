using System.Collections.Generic;
using HotChocolate.Language.Rewriters.Contracts;

namespace HotChocolate.Language.Rewriters;

public readonly struct SyntaxReference : ISyntaxReference
{
    public SyntaxReference(ISyntaxReference? parent, ISyntaxNode node)
    {
        Parent = parent;
        Node = node;
    }

    public ISyntaxReference? Parent { get; }
    public ISyntaxNode Node { get; }

    public T? GetAncestor<T>()
        where T : ISyntaxNode
    {
        ISyntaxReference? current = this;
        while (current is not null)
        {
            if (current.Node is T typedReference)
            {
                return typedReference;
            }

            current = current.Parent;
        }

        return default;
    }

    public IEnumerable<T> GetAncestors<T>()
        where T : ISyntaxNode
    {
        ISyntaxReference? current = this;
        while (current is not null)
        {
            if (current.Node is T typedReference)
            {
                yield return typedReference;
            }

            current = current.Parent;
        }
    }
}
