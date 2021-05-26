namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class SortableDirective
    {
        public SortableDirective(SortDirection direction)
        {
            Direction = direction;
        }

        public SortDirection Direction { get; }
    }
}
