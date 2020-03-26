namespace HotChocolate.Language.Visitors
{
    public interface ISyntaxVisitor
    {
        ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            ISyntaxVisitorContext context);
    }

    public interface ISyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            TContext context);
    }
}
