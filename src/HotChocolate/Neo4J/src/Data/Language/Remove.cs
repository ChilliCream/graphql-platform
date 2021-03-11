namespace HotChocolate.Data.Neo4J.Language
{
    public class Remove : Visitable, IUpdatingClause
    {
        public override ClauseKind Kind { get; } = ClauseKind.Remove;
        private readonly ExpressionList _setItems;

        public Remove(ExpressionList setItems)
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
