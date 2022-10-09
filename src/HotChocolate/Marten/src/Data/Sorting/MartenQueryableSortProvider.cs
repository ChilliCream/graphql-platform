using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using Marten.Linq;

namespace HotChocolate.Data.Marten.Sorting;

public class MartenQueryableSortProvider : QueryableSortProvider
{
    public MartenQueryableSortProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure) : base(configure)
    {
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input) &&
            input is not IMartenQueryable<TEntityType> and not IMartenQueryable;
}
