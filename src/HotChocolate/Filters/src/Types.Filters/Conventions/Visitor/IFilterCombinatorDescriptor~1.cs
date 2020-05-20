namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterCombinatorDescriptor<T> : IFluent
    {
        IFilterCombinatorDescriptor<T> Handler(
            FilterOperationCombinator<T> operationCombinator);

        IFilterVisitorDescriptor<T> And();
    }
}
