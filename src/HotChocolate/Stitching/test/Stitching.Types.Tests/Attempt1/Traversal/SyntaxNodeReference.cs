using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Traversal;

public readonly struct SyntaxNodeReference : ISyntaxNodeReference
{
    public SyntaxNodeReference(ISyntaxNodeReference? parent, ISyntaxNode node)
    {
        Parent = parent;
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public ISyntaxNodeReference? Parent { get; }

    public ISyntaxNode Node { get; }
}
