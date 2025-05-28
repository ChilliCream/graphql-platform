namespace HotChocolate.Fusion.Language;

internal class SkipSyntaxVisitorAction : ISyntaxVisitorAction
{
    public SyntaxVisitorActionKind Kind => SyntaxVisitorActionKind.Skip;
}
