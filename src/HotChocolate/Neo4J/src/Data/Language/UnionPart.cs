namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a part on an union
    /// </summary>
    public class UnionPart : Visitable
    {
        public override ClauseKind Kind => ClauseKind.UnionPart;

        private readonly bool _all;
        private readonly SingleQuery _query;

        public UnionPart(bool all, SingleQuery query)
        {
            _all = all;
            _query = query;
        }

        public bool IsAll() => _all;

        public SingleQuery GetQuery() => _query;

        public new void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _query.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
