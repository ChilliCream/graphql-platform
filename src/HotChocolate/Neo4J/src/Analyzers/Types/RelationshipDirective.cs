namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class RelationshipDirective
    {
        public RelationshipDirective(string type, RelationshipDirection direction)
        {
            Type = type;
            Direction = direction;
        }

        public string Type { get; }

        public RelationshipDirection Direction { get; }
    }
}
