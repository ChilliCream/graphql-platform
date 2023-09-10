namespace HotChocolate.Language.Visitors;

public class BreakSyntaxVisitorAction : ISyntaxVisitorAction
{
    public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.Break;
}
