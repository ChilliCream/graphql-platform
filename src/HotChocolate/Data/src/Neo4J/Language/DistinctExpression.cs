using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    ///
    /// </summary>
    public class DistinctExpression : Expression
    {
        public override ClauseKind Kind => ClauseKind.Default;
        private readonly Expression _expression;

        public DistinctExpression(Expression expression)
        {
            _expression = expression;
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            Distinct.Instance.Visit(visitor);
            _expression.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
