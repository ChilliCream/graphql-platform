namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Create.html">Create</a>.
    /// Create = (C,R,E,A,T,E), [SP], Pattern ;
    /// </summary>
    public class Create : Visitable, IUpdatingClause
    {
        public override ClauseKind Kind => ClauseKind.Create;

        private readonly Pattern _pattern;

        public Create(Pattern pattern)
        {
            _pattern = pattern;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _pattern.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
