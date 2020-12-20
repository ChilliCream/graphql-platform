namespace HotChocolate.Data.Neo4J.Language
{
    public class ReturnClause : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Return;
        private readonly Distinct? _distinct;
        private readonly ProjectionBody _returnBody;

        public ReturnClause(bool distinct, ExpressionList returnItems, Order order, Skip skip, Limit limit) {
            _distinct = distinct ? new Distinct(true) : null;
            _returnBody = new ProjectionBody(returnItems, order, skip, limit);
        }

        public new void Visit(CypherVisitor visitor) {

            visitor.Enter(this);
            _distinct?.Visit(visitor);
            _returnBody.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
