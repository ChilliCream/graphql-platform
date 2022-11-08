using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using Raven.Linq;

namespace HotChocolate.Data.Raven.Sorting;

public class RavenQueryableSortProvider : QueryableSortProvider
{
    public RavenQueryableSortProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure) : base(configure)
    {
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input) &&
            input is not IRavenQueryable<TEntityType> and not IRavenQueryable;
}
