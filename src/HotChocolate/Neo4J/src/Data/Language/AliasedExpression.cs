namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// An aliased expression, that deals with named expressions when accepting visitors.
    /// </summary>
    public class AliasedExpression
        : Expression
        , IAliased
    {
        public AliasedExpression(Expression expression, string alias)
        {
            Expression = expression;
            Alias = alias;
        }

        public override ClauseKind Kind => ClauseKind.AliasedExpression;

        public string Alias { get; }

        public Expression Expression { get; }

        public SymbolicName AsName() => SymbolicName.Of(Alias);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(Expression).Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
