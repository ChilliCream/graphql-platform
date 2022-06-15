using System;

namespace HotChocolate.Language.Visitors;

[Serializable]
public class SyntaxNodeCannotBeNullException : Exception
{
    public SyntaxNodeCannotBeNullException(ISyntaxNode node)
    {
        Kind = node.Kind;
        Location = node.Location;
    }

    public SyntaxKind Kind { get; set; }

    public Location? Location { get; }
}
