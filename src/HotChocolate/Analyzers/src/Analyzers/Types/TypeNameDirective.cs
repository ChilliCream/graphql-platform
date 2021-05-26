namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class TypeNameDirective
    {
        public string Name { get; set; } = default!;

        public string? PluralName { get; set; } = default!;
    }
}
