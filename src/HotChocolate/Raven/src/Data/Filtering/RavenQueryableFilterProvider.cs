using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using Raven.Linq;

namespace HotChocolate.Data.Raven.Filtering;

public class RavenQueryableFilterProvider : QueryableFilterProvider
{
    public RavenQueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure) : base(configure)
    {
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input) &&
            input is not IRavenQueryable<TEntityType> and not IRavenQueryable;
}
