namespace HotChocolate.Language.Visitors
{
    internal static class SyntaxVisitorActionExtension
    {
        public static bool IsBreak(this ISyntaxVisitorAction action)
        {
            return action.Kind == SyntaxVisitorActionKind.Break;
        }
    }
}
