namespace Prometheus.Language
{
    public partial class SyntaxNodeVisitor
    {
        protected SyntaxNodeVisitor()
        {

        }

        public virtual void Visit(ISyntaxNode node)
        {
            if (node != null && _visitationMap.TryGetValue(node.Kind, out var visitMethod))
            {
                visitMethod(this, node);
            }
        }
    }
}