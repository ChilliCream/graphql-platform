namespace HotChocolate.Analyzers.Types
{
    public class RelationshipDirective
    {
        public string Type { get; set; } = default!;

        public RelationshipDirection Direction { get; set; } = default!;
    }
}
