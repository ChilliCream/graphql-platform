namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A list predicate.
    /// </summary>
    public class ListPredicate : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.ListPredicate;

        private readonly SymbolicName _variable;
        private readonly Expression _listExpression;
        private readonly Where _where;

        public ListPredicate(SymbolicName variable, Expression listExpression, Where where)
        {
            _variable = variable;
            _listExpression = listExpression;
            _where = where;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _variable.Visit(cypherVisitor);
            Operator.In.Visit(cypherVisitor);
            _listExpression.Visit(cypherVisitor);
            _where.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
