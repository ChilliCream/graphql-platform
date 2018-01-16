using GraphQLParser.AST;

namespace Zeus
{
    public static class ASTNodeExtensions
    {
        public static void Accept(this ASTNode node, SyntaxNodeVisitor visitor)
        {
            visitor.Visit(node);
        }
    }
}
