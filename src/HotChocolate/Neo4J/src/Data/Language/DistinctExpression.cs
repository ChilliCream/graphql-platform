using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// AST representation of the DISTINCT keyword.
    /// </summary>
    public class DistinctExpression : Expression
    {
        public override ClauseKind Kind => ClauseKind.DistinctExpression;
        private readonly Expression _expression;

        public DistinctExpression(Expression expression)
        {
            _expression = expression;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Distinct.Instance.Visit(cypherVisitor);
            _expression.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
