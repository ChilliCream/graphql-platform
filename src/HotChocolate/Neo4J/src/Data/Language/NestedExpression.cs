namespace HotChocolate.Data.Neo4J.Language
{
    public class NestedExpression : Expression
    {
        public override ClauseKind Kind => ClauseKind.NestedExpression;
        private readonly Expression _expression;

        public NestedExpression(Expression expression)
        {
            _expression = expression;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(_expression).Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
