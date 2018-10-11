namespace HotChocolate.Language
{
    public partial class SyntaxNodeVisitor<TStart>
        where TStart : ISyntaxNode
    {
        protected SyntaxNodeVisitor() { }

        public virtual void Visit(TStart node)
        {
        }
    }
}
