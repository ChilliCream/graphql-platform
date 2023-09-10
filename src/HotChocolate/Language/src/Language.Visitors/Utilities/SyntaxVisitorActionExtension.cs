namespace HotChocolate.Language.Visitors;

public static class SyntaxVisitorActionExtension
{
    public static bool IsBreak(this ISyntaxVisitorAction action)
        => action.Kind == SyntaxVisitorActionKind.Break;

    public static bool IsContinue(this ISyntaxVisitorAction action)
        => action.Kind == SyntaxVisitorActionKind.Continue;

    public static bool IsSkip(this ISyntaxVisitorAction action)
        => action.Kind == SyntaxVisitorActionKind.Skip;
}
