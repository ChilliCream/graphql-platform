namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// The container or "body" for return items, order and optional skip things.
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/ProjectionBody.html">
    /// Projection Body
    /// </a>
    /// </summary>
    public class ProjectionBody : Visitable
    {
        private readonly ExpressionList _returnItems;
        private readonly OrderBy? _order;
        private readonly Skip? _skip;
        private readonly Limit? _limit;

        public ProjectionBody(
            ExpressionList returnItems,
            OrderBy? order,
            Skip? skip,
            Limit? limit)
        {
            _returnItems = returnItems;
            _order = order;
            _skip = skip;
            _limit = limit;
        }

        public override ClauseKind Kind => ClauseKind.ProjectionBody;

        public new void Visit(CypherVisitor cypherVisitor)
        {
            _returnItems.Visit(cypherVisitor);
            _order?.Visit(cypherVisitor);
            _skip?.Visit(cypherVisitor);
            _limit?.Visit(cypherVisitor);
        }
    }
}
