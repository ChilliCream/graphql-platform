namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// An aliased expression, that deals with named expressions when accepting visitors.
    /// </summary>
    public class AliasedExpression
        : Expression
        , IAliased
    {
        private readonly Expression _expression;

        public AliasedExpression(Expression expression, string alias)
        {
            _expression = expression;
            Alias = alias;
        }

        public override ClauseKind Kind => ClauseKind.AliasedExpression;

        public string Alias { get; }

        public SymbolicName AsName() => SymbolicName.Of(Alias);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(_expression).Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
