namespace HotChocolate.Language.Visitors
{
    public static class SyntaxVisitorActionExtension
    {
        public static bool IsBreak(this ISyntaxVisitorAction action)
        {
            return action.Kind == SyntaxVisitorActionKind.Break;
        }
        public static bool IsContinue(this ISyntaxVisitorAction action)
        {
            return action.Kind == SyntaxVisitorActionKind.Continue;
        }
        public static bool IsSkip(this ISyntaxVisitorAction action)
        {
            return action.Kind == SyntaxVisitorActionKind.Skip;
        }
    }
}
