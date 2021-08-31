namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Match.html
    /// Match = [(O,P,T,I,O,N,A,L), SP], (M,A,T,C,H), [SP], Pattern, [[SP], Where] ;
    /// </summary>
    public class Match : Visitable, IReadingClause
    {
        public Match(bool isOptional, Pattern pattern, Where? where)
        {
            IsOptional = isOptional;
            Pattern = pattern;
            Where = where;
        }

        public override ClauseKind Kind => ClauseKind.Match;

        public bool IsOptional { get;}

        public Pattern Pattern { get;}

        public Where? Where { get;}

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Pattern.Visit(cypherVisitor);
            Where?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
