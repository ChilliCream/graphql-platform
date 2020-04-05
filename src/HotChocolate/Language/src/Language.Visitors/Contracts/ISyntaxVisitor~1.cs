namespace HotChocolate.Language.Visitors
{
    public interface ISyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            TContext context);
    }
}
