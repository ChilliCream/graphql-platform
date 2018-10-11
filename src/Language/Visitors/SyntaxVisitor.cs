namespace HotChocolate.Language
{
    public partial class SyntaxVisitor<TStart>
        where TStart : ISyntaxNode
    {
        protected SyntaxVisitor() { }

        public virtual void Visit(TStart node)
        {
        }
    }
}
