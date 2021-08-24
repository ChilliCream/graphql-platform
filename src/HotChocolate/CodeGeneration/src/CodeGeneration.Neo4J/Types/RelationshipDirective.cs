namespace HotChocolate.CodeGeneration.Neo4J.Types
{
    public class RelationshipDirective
    {
        public string Name { get; set; } = default!;

        public RelationshipDirection Direction { get; set; } = default!;
    }
}
