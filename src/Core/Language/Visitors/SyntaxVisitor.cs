namespace HotChocolate.Language
{
    public partial class SyntaxVisitor<TStart, TContext>
        where TStart : ISyntaxNode
    {
        protected SyntaxVisitor() { }

        public virtual void Visit(TStart node, TContext context) { }
    }
}
