namespace HotChocolate.Fusion;

internal static class SyntaxVisitorActionExtensions
{
    public static bool IsBreak(this ISyntaxVisitorAction action)
        => action.Kind == SyntaxVisitorActionKind.Break;
}
