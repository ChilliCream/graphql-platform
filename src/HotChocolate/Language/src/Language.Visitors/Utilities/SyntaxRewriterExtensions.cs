namespace HotChocolate.Language.Visitors;

public static class SyntaxRewriterExtensions
{
    private static readonly EmptySyntaxVisitorContext _empty = new();

    public static ISyntaxNode Rewrite(
        this ISyntaxRewriter<ISyntaxVisitorContext> rewriter,
        ISyntaxNode node)
        => rewriter.Rewrite(node, _empty);

    public static T Rewrite<T>(
        this ISyntaxRewriter<ISyntaxVisitorContext> rewriter,
        T node)
        where T : ISyntaxNode
        => (T)rewriter.Rewrite(node, _empty);

    private sealed class EmptySyntaxVisitorContext : ISyntaxVisitorContext
    {
    }
}
