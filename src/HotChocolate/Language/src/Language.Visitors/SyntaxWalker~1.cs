namespace HotChocolate.Language.Visitors;

public partial class SyntaxWalker<TContext> : SyntaxVisitor<TContext>
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
