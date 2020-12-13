namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/With.html
    /// </summary>
    public class WithClause : Visitable
    {
        public override ClauseKind Kind => ClauseKind.With;

        private readonly Distinct _distinct;
        private readonly ProjectionBody _projectionBody;
        private readonly Where _where;

        public WithClause(bool distinct, ExpressionList returnItems, Order order, Skip skip, Limit limit, Where where)
        {
            _distinct = new Distinct(distinct);
            _projectionBody = new ProjectionBody(returnItems, order, skip, limit);
            _where = where;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            VisitIfNotNull(_distinct, visitor);
            _projectionBody.Visit(visitor);
            VisitIfNotNull(_where, visitor);
            visitor.Leave(this);
        }
    }
}
