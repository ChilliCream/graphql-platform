namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <see href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/With.html" />
    /// </summary>
    public class With : Visitable
    {
        public override ClauseKind Kind => ClauseKind.With;
        private readonly ProjectionBody _body;
        private readonly Where _where;

        public With(
            bool distinct,
            ExpressionList returnItems,
            OrderBy orderBy,
            Skip skip,
            Limit limit,
            Where where)
        {
            _body = new ProjectionBody(distinct ,returnItems, orderBy, skip, limit);
            _where = where;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _body.Visit(cypherVisitor);
            _where?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
