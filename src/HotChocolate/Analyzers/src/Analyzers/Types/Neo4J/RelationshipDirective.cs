namespace HotChocolate.Analyzers.Types.Neo4J
{
    public class RelationshipDirective
    {
        public string Name { get; set; } = default!;

        public RelationshipDirection Direction { get; set; } = default!;
    }
}
