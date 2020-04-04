namespace HotChocolate.Language.Visitors
{
    public static class SyntaxVisitorActionExtension
    {
        public static bool IsBreak(this ISyntaxVisitorAction action)
        {
            return action.Kind == SyntaxVisitorActionKind.Break;
        }
    }
}
