namespace HotChocolate.Language
{
    public static class DocumentRewriterExtensions
    {
        public static DocumentNode Rewrite<TRewriter, TContext>(
            this DocumentNode node, TContext context)
            where TRewriter : QuerySyntaxRewriter<TContext>, new()
        {
            var rewriter = new TRewriter();
            return (DocumentNode)rewriter.Rewrite(node, context);
        }

        public static T Rewrite<T, TContext>(
            this QuerySyntaxRewriter<TContext> rewriter,
            T node,
            TContext context)
            where T : ISyntaxNode
        {
            return (T)rewriter.Rewrite(node, context);
        }
    }
}
