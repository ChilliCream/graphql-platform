namespace HotChocolate.Fusion;

internal class SkipSyntaxVisitorAction : ISyntaxVisitorAction
{
    public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.Skip;
}
