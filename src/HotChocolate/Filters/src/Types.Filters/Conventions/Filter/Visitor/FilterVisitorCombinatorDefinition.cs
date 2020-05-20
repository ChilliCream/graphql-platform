namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorCombinatorDefinition<T>
    {
        public FilterCombinator Combinator { get; set; }

        public FilterOperationCombinator<T>? Handler { get; set; }
    }
}
