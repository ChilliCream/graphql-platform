namespace StrawberryShake.VisualStudio.Language
{
    public interface ISyntaxVisitor
    {
        ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            ISyntaxVisitorContext context);
    }

    public interface ISyntaxVisitorAction
    {
    }

    public interface ISyntaxVisitorContext
    {

    }
}
