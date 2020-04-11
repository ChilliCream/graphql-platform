namespace HotChocolate.Language.Visitors
{
    public class SkipSyntaxVisitorAction : ISyntaxVisitorAction
    {
        public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.Skip;
    }
}
