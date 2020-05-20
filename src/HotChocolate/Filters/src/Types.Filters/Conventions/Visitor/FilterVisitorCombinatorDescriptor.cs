namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorCombinatorDescriptor<T> : IFilterCombinatorDescriptor<T>
    {
        private readonly IFilterVisitorDescriptor<T> _descriptor;

        public FilterVisitorCombinatorDescriptor(
            IFilterVisitorDescriptor<T> descriptor,
            FilterCombinator combinator)
        {
            _descriptor = descriptor;
            Definition.Combinator = combinator;
        }

        protected FilterVisitorCombinatorDefinition<T> Definition { get; } =
            new FilterVisitorCombinatorDefinition<T>();

        public FilterVisitorCombinatorDefinition<T> CreateDefinition() => Definition;

        public IFilterVisitorDescriptor<T> And() => _descriptor;

        public IFilterCombinatorDescriptor<T> Handler(
            FilterOperationCombinator<T> operationCombinator)
        {
            Definition.Handler = operationCombinator;
            return this;
        }

        public static FilterVisitorCombinatorDescriptor<T> New(
            IFilterVisitorDescriptor<T> descriptor,
            FilterCombinator combinator) =>
            new FilterVisitorCombinatorDescriptor<T>(descriptor, combinator);
    }
}
