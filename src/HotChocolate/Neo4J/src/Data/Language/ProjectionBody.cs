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
        public ProjectionBody(
            ExpressionList returnItems,
            OrderBy? orderBy,
            Skip? skip,
            Limit? limit)
        {
            ReturnItems = returnItems;
            OrderBy = orderBy;
            Skip = skip;
            Limit = limit;
        }

        public override ClauseKind Kind => ClauseKind.ProjectionBody;

        public ExpressionList ReturnItems { get; }

        public OrderBy? OrderBy { get; }

        public Skip? Skip { get; }

        public Limit? Limit { get; }

        public new void Visit(CypherVisitor cypherVisitor)
        {
            ReturnItems.Visit(cypherVisitor);
            OrderBy?.Visit(cypherVisitor);
            Skip?.Visit(cypherVisitor);
            Limit?.Visit(cypherVisitor);
        }
    }
}
