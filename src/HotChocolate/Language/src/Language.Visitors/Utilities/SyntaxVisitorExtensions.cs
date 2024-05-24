namespace HotChocolate.Language.Visitors;

public static class SyntaxVisitorExtensions
{
    private static readonly object _empty = new();

    public static ISyntaxVisitorAction Visit(
        this ISyntaxVisitor<object?> visitor,
        ISyntaxNode node)
        => visitor.Visit(node, _empty);
}
