namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A list predicate.
    /// </summary>
    public class ListPredicate : Expression
    {
        public ListPredicate(SymbolicName variable, Expression listExpression, Where where)
        {
            Variable = variable;
            ListExpression = listExpression;
            Where = where;
        }

        public override ClauseKind Kind => ClauseKind.ListPredicate;

        public SymbolicName Variable { get; }

        public Expression ListExpression { get; }

        public Where Where { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Variable.Visit(cypherVisitor);
            Operator.In.Visit(cypherVisitor);
            ListExpression.Visit(cypherVisitor);
            Where.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
