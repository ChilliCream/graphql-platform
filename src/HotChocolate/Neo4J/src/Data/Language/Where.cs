namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// WHERE adds constraints to the patterns in a MATCH or OPTIONAL MATCH clause or filters the
    /// results of a WITH clause.
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Match.html#Where" >
    /// Where
    /// </a>
    /// </summary>
    public class Where : Visitable
    {
        public Where(Condition condition)
        {
            Exists = null;
            Condition = condition;
        }

        public Where(bool exists, Condition condition)
        {
            Exists = exists ? Exists.Instance : null;
            Condition = condition;
        }

        public override ClauseKind Kind => ClauseKind.Where;

        public Exists Exists { get; }

        public Condition Condition { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Exists?.Visit(cypherVisitor);
            Condition.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
