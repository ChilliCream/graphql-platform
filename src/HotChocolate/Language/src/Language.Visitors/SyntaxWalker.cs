namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker : SyntaxVisitor
    {
        protected SyntaxWalker()
        {
        }

        protected SyntaxWalker(ISyntaxVisitorAction defaultResult)
            : base(defaultResult)
        {
        }
    }
}
