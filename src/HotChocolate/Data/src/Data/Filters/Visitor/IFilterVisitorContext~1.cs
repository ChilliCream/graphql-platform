using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContext
    {
        IStackableList<FilterScope<T>> Scopes { get; }

        FilterScope<T> CreateScope();
    }
}