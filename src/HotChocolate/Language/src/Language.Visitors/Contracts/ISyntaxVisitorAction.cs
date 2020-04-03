namespace HotChocolate.Language.Visitors
{
    public interface ISyntaxVisitorAction
    {
        SyntaxVisitorActionKind Kind { get; }
    }
}
