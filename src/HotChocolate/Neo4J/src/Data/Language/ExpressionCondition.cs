namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A condition that uses its bound expression
    /// </summary>
    public class ExpressionCondition : Condition
    {
        public override ClauseKind Kind { get; } = ClauseKind.Default;
        private readonly Expression _expression;

        public ExpressionCondition(Expression expression)
        {
            _expression = expression;
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _expression.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
