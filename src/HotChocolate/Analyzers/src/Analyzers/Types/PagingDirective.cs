namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class PagingDirective
    {
        public PagingKind Kind { get; set; } = default!;

        public int DefaultPageSize { get; set; } = default!;

        public int MaxPageSize { get; set; } = default!;

        public bool IncludeTotalCount { get; set; } = default!;
    }
}
