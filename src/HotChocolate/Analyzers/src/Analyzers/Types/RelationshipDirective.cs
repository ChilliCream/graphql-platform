namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class RelationshipDirective
    {
        public string Type { get; set; } = default!;

        public RelationshipDirection Direction { get; set; } = default!;
    }
}
