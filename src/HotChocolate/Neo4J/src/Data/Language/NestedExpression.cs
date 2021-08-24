namespace HotChocolate.Data.Neo4J.Language
{
    public class NestedExpression : Expression
    {
        public NestedExpression(Expression expression)
        {
            Expression = expression;
        }

        public override ClauseKind Kind => ClauseKind.NestedExpression;

        public Expression Expression { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(Expression).Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
