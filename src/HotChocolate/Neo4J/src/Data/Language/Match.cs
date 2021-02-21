namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Match.html
    /// Match = [(O,P,T,I,O,N,A,L), SP], (M,A,T,C,H), [SP], Pattern, [[SP], Where] ;
    /// </summary>
    public class Match : Visitable, IReadingClause
    {
        public override ClauseKind Kind => ClauseKind.Match;
        private readonly bool _optional;
        private readonly Pattern _pattern;
        private readonly Where _optionalWhere;

        public Match(bool optional, Pattern pattern, Where optionalWhere)
        {
            _optional = optional;
            _pattern = pattern;
            _optionalWhere = optionalWhere;
        }

        public bool IsOptional() => _optional;

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _pattern.Visit(visitor);
            _optionalWhere?.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
