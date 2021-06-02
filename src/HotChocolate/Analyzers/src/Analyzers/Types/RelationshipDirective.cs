namespace HotChocolate.Analyzers.Types
{
    public class RelationshipDirective
    {
        public string Name { get; set; } = default!;

        public RelationshipDirection Direction { get; set; } = default!;
    }
}
