namespace HotChocolate.Language.Visitors;

public static class SyntaxRewriterExtensions
{
    private static readonly object? _empty = new();

    public static ISyntaxNode? Rewrite(
        this ISyntaxRewriter<object?> rewriter,
        ISyntaxNode node)
        => rewriter.Rewrite(node, _empty);

    public static T? Rewrite<T>(
        this ISyntaxRewriter<object?> rewriter,
        T node)
        where T : ISyntaxNode
        => (T?)rewriter.Rewrite(node, _empty);
}
