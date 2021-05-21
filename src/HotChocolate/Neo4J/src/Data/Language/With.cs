namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/With.html" >
    /// With
    /// </a>
    /// </summary>
    public class With : Visitable
    {
        public With(
            ExpressionList returnItems,
            OrderBy orderBy,
            Skip skip,
            Limit limit,
            Where where)
        {
            Body = new ProjectionBody(returnItems, orderBy, skip, limit);
            Where = where;
        }

        public override ClauseKind Kind => ClauseKind.With;

        public ProjectionBody Body { get; }

        public Where Where { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Body.Visit(cypherVisitor);
            Where?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
