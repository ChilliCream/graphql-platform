namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents properties of a node or a relationship.
    /// </summary>
    public class Properties : Visitable
    {
        public Properties(MapExpression members)
        {
            Members = members;
        }

        public override ClauseKind Kind => ClauseKind.Properties;

        public MapExpression Members { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Members.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
