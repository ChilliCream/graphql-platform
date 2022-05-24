namespace HotChocolate.Language.Visitors;

public interface ISyntaxRewriter<in TContext> where TContext : ISyntaxVisitorContext
{
    ISyntaxNode Rewrite(ISyntaxNode node, TContext context);
}
