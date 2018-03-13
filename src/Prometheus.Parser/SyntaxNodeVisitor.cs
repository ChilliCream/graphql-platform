using System;
using GraphQLParser.AST;

namespace Prometheus
{
    public partial class SyntaxNodeVisitor
    {
        protected SyntaxNodeVisitor()
        {

        }

        public virtual void Visit(ASTNode node)
        {
            if (node != null && _visitationMap.TryGetValue(node.Kind, out var visitMethod))
            {
                visitMethod(this, node);
            }
        }
    }

    public partial class SyntaxNodeVisitor<TContext>
    {
        protected SyntaxNodeVisitor()
        {

        }

        public virtual void Visit(ASTNode node, TContext context)
        {
            if (node != null && _visitationMap.TryGetValue(node.Kind, out var visitMethod))
            {
                visitMethod(this, node, context);
            }
        }
    }
}
