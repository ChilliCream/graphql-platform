using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using Marten.Linq;

namespace HotChocolate.Data.Marten.Filtering;

public class MartenQueryableFilterProvider : QueryableFilterProvider
{
    public MartenQueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure) : base(configure)
    {
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input) &&
            input is not IMartenQueryable<TEntityType> and not IMartenQueryable;
}
