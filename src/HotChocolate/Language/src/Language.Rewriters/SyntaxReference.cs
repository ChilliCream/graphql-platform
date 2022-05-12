using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters;

public sealed class SyntaxReference
{
    public SyntaxReference(SyntaxReference? parent, ISyntaxNode node)
    {
        Parent = parent;
        Node = node;
    }

    public SyntaxReference? Parent { get; }
    public ISyntaxNode Node { get; }

    public T? GetAncestor<T>()
        where T : ISyntaxNode
    {
        SyntaxReference? current = this;
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
        SyntaxReference? current = this;
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
