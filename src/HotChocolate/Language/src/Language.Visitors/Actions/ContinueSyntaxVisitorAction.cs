namespace HotChocolate.Language.Visitors;

public class ContinueSyntaxVisitorAction : ISyntaxVisitorAction
{
    public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.Continue;
}
