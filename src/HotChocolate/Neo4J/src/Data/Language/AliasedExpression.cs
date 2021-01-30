using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// An aliased expression, that deals with named expressions when accepting visitors.
    /// </summary>
    public class AliasedExpression : Expression, IAliased
    {
        public override ClauseKind Kind => ClauseKind.AliasedExpression;

        private readonly string _alias;
        private readonly Expression _expression;

        public AliasedExpression(Expression expression, string alias)
        {
            _expression = expression;
            _alias = alias;
        }

        public string GetAlias() => _alias;

        public SymbolicName AsName() => SymbolicName.Of(GetAlias());
        public Expression GetExpression() => _expression;

        public new AliasedExpression As(string newAlias)
        {
            _ = newAlias ??
                throw new ArgumentNullException(nameof(newAlias));
            return new AliasedExpression(_expression, newAlias);
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            Expressions.NameOrExpression(_expression).Visit(visitor);
            visitor.Leave(this);
        }
    }
}
