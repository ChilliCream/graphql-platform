namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class PagingDirective
    {
        public PagingDirective(
            PagingKind kind,
            int? defaultPageSize,
            int? maxPageSize,
            bool? includeTotalCount)
        {
            Kind = kind;
            DefaultPageSize = defaultPageSize ?? 10;
            MaxPageSize = maxPageSize ?? 50;
            IncludeTotalCount = includeTotalCount ?? false;
        }

        public PagingKind Kind { get; }

        public int DefaultPageSize { get; }

        public int MaxPageSize { get; }

        public bool IncludeTotalCount { get; }
    }
}
