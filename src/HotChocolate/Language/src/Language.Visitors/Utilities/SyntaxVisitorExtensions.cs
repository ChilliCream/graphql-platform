namespace HotChocolate.Language.Visitors;

public static class SyntaxVisitorExtensions
{
    private static readonly object s_empty = new();

    public static ISyntaxVisitorAction Visit(
        this ISyntaxVisitor<object?> visitor,
        ISyntaxNode node)
        => visitor.Visit(node, s_empty);
}
