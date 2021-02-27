namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Unwind.html
    /// </summary>
    public class Unwind : Visitable, IReadingClause
    {
        public override ClauseKind Kind => ClauseKind.Unwind;
        private readonly Expression _expressionToUnwind;
        private readonly string _variable;

        public Unwind(Expression expressionToUnwind, string variable)
        {
            _expressionToUnwind = _expressionToUnwind is IAliased ? ((IAliased)expressionToUnwind).AsName() : expressionToUnwind;
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
