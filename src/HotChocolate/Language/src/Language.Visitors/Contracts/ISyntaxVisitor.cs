namespace HotChocolate.Language.Visitors
{
    public interface ISyntaxVisitor
    {
        ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            ISyntaxVisitorContext context);
    }
}
