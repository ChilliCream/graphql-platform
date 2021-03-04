namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A condition that uses its bound expression
    /// </summary>
    public class ExpressionCondition : Condition
    {
        public override ClauseKind Kind { get; } = ClauseKind.ExpressionCondition;
        private readonly Expression _expression;

        public ExpressionCondition(Expression expression)
        {
            _expression = expression;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _expression.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
