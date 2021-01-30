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
        private readonly Exists? _exists;

        public Where(Condition condition)
        {
            _exists = null;
            _condition = condition;
        }
        public Where(bool exists, Condition condition)
        {
            _exists = exists ? Exists.Instance : null;
            _condition = condition;
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _exists?.Visit(visitor);
            _condition.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
