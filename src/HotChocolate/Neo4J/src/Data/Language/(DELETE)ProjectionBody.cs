namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/ProjectionBody.html
    /// </summary>
    public class ProjectionBody : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Default;
        private readonly ExpressionList _returnItems;
        private readonly OrderBy? _order;
        private readonly Skip? _skip;
        private readonly Limit? _limit;

        public ProjectionBody(ExpressionList returnItems, OrderBy? order, Skip? skip, Limit? limit)
        {
            _returnItems = returnItems;
            _order = order;
            _skip = skip;
            _limit = limit;
        }

        public new void Visit(CypherVisitor visitor)
        {
            _returnItems.Visit(visitor);
            _order?.Visit(visitor);
            _skip?.Visit(visitor);
            _limit?.Visit(visitor);
        }
    }
}
