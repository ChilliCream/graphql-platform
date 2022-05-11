using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public readonly struct SyntaxReference : ISyntaxReference
{
    public SyntaxReference(ISyntaxReference? parent, ISyntaxNode node)
    {
        Parent = parent;
        Node = node;
    }

    public ISyntaxReference? Parent { get; }
    public ISyntaxNode Node { get; }
}