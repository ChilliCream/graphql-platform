#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// The RETURN clause defines what to include in the query result set.
    /// </summary>
    public class Return : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Return;
        private readonly ProjectionBody _returnItems;

        public Return(
            bool distinct,
            ExpressionList returnItems,
            OrderBy? orderBy = null,
            Skip? skip = null,
            Limit? limit = null)
        {
            _returnItems = new ProjectionBody(distinct, returnItems, orderBy, skip, limit);
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _returnItems.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
