namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A negated version of the condition passed during construction of this condition.
    /// </summary>
    public class NotCondition : Condition
    {
        public override ClauseKind Kind { get; } = ClauseKind.NotCondition;

        private readonly Condition _condition;

        public NotCondition(Condition condition)
        {
            _condition = condition;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _condition.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
