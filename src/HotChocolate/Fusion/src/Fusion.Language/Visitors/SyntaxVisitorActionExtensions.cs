namespace HotChocolate.Fusion.Language;

internal static class SyntaxVisitorActionExtensions
{
    public static bool IsBreak(this ISyntaxVisitorAction action)
        => action.Kind == SyntaxVisitorActionKind.Break;
}
