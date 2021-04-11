namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Set.html
    /// </summary>
    public class Set : Visitable, IUpdatingClause
    {
        public override ClauseKind Kind => ClauseKind.Set;
        private readonly ExpressionList _setItems;

        public Set(ExpressionList setItems)
        {
            _setItems = setItems;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _setItems.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
