namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Match.html#Where
    /// Where = (W,H,E,R,E), SP, Expression ;
    /// </summary>
    public class Where : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Where;
        private readonly Condition _condition;

        public Where(Condition condition)
        {
            _condition = condition;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _condition.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
