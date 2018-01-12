using System;
using GraphQLParser.AST;

namespace Zeus
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
}
