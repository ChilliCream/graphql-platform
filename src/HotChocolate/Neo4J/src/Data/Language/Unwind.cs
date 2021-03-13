namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// With UNWIND, you can transform any list back into individual rows.
    /// These lists can be parameters that were passed in, previously collected result or other list expressions.
    ///
    /// One common usage of unwind is to create distinct lists. Another is to create data from parameter lists that are provided to the query.
    /// <see href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Unwind.html" />
    /// </summary>
    public class Unwind : Visitable, IReadingClause
    {
        public override ClauseKind Kind => ClauseKind.Unwind;
        private readonly Expression _expressionToUnwind;
        private readonly string _variable;

        public Unwind(Expression expressionToUnwind, string variable)
        {
            _expressionToUnwind = _expressionToUnwind is IAliased
                ? ((IAliased)expressionToUnwind).AsName()
                : expressionToUnwind;
            _variable = variable;
        }

        public string GetVariable() => _variable;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _expressionToUnwind.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
