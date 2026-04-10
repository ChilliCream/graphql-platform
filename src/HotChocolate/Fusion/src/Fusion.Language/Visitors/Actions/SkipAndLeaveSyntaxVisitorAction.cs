namespace HotChocolate.Fusion.Language;

internal class SkipAndLeaveSyntaxVisitorAction : ISyntaxVisitorAction
{
    public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.SkipAndLeave;
}
