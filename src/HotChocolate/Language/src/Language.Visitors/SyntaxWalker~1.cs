namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker<TContext>
        : SyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        protected SyntaxWalker() : base(Continue)
        {
        }

        protected SyntaxWalker(ISyntaxVisitorAction defaultResult)
            : base(defaultResult)
        {
        }
    }
}
