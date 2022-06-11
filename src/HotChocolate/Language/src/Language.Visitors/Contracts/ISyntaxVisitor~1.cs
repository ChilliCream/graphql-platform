namespace HotChocolate.Language.Visitors;

public interface ISyntaxVisitor<in TContext> where TContext : ISyntaxVisitorContext
{
    ISyntaxVisitorAction Visit(ISyntaxNode node, TContext context);
}
