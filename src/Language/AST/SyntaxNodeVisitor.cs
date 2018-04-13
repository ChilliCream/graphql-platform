using System.Collections.Generic;

namespace HotChocolate.Language
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

        protected virtual void VisitMany(IEnumerable<ISyntaxNode> nodes)
        {
            if (nodes != null)
            {
                foreach (ISyntaxNode node in nodes)
                {
                    Visit(node);
                }
            }
        }
    }
}