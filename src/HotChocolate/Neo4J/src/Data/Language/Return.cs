namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// The RETURN clause defines what to include in the query result set.
    /// </summary>
    public class Return : Visitable
    {
        public Return(
            ExpressionList returnItems,
            OrderBy? orderBy = null,
            Skip? skip = null,
            Limit? limit = null)
        {
            Items = new ProjectionBody(returnItems, orderBy, skip, limit);
        }

        public override ClauseKind Kind => ClauseKind.Return;

        public ProjectionBody Items { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Items.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
