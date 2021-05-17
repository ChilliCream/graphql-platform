﻿namespace HotChocolate.Data.Neo4J.Language
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
        private readonly Exists _exists;
        private readonly Condition _condition;

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

        public override ClauseKind Kind => ClauseKind.Where;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _exists?.Visit(cypherVisitor);
            _condition.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
