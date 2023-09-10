namespace HotChocolate.Language.Visitors;

public class SkipAndLeaveSyntaxVisitorAction : ISyntaxVisitorAction
{
    public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.SkipAndLeave;
}
