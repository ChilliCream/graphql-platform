using GraphQLParser.AST;

namespace Prometheus.Parser
{
    public static class ASTNodeExtensions
    {
        public static void Accept(this ASTNode node, SyntaxNodeVisitor visitor)
        {
            if (node == null)
            {
                throw new System.ArgumentNullException(nameof(node));
            }

            if (visitor == null)
            {
                throw new System.ArgumentNullException(nameof(visitor));
            }

            visitor.Visit(node);
        }
    }
}
