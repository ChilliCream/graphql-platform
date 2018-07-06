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
            if (node != null)
            {
                ExecuteVisitationMap(node);
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