namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Unwind.html
    /// </summary>
    public class Unwind : Visitable, IReadingClause
    {
        public new ClauseKind Kind { get; } = ClauseKind.Unwind;
        private readonly Expression _expressionToUnwind;
        private readonly string _variable;

        public Unwind(Expression expressionToUnwind, string variable)
        {
            if (_expressionToUnwind is IAliased)
            {
                _expressionToUnwind = ((IAliased)expressionToUnwind).AsName();
            }
            else
            {
                _expressionToUnwind = expressionToUnwind;
            }

            _expressionToUnwind = expressionToUnwind;
            _variable = variable;
        }

        public string GetVariable() => _variable;

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _expressionToUnwind.Visit(visitor);
            visitor.Leave(this);
        }
    }
}