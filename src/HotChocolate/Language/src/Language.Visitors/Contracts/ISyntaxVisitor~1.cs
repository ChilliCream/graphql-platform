namespace HotChocolate.Language.Visitors;

public interface ISyntaxVisitor<in TContext>
{
    ISyntaxVisitorAction Visit(ISyntaxNode node, TContext context);
}
