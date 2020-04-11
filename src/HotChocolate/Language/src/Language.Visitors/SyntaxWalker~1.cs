namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker<TContext>
        : SyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        protected SyntaxWalker(SyntaxVisitorOptions options = default)
            : base(Continue, options)
        {
        }

        protected SyntaxWalker(
            ISyntaxVisitorAction defaultResult,
            SyntaxVisitorOptions options = default)
            : base(defaultResult, options)
        {
        }
    }
}
