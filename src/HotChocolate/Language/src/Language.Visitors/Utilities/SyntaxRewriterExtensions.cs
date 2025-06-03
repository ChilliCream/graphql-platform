namespace HotChocolate.Language.Visitors;

public static class SyntaxRewriterExtensions
{
    private static readonly object? s_empty = new();

    public static ISyntaxNode? Rewrite(
        this ISyntaxRewriter<object?> rewriter,
        ISyntaxNode node)
        => rewriter.Rewrite(node, s_empty);

    public static T? Rewrite<T>(
        this ISyntaxRewriter<object?> rewriter,
        T node)
        where T : ISyntaxNode
        => (T?)rewriter.Rewrite(node, s_empty);
}
