using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Raven.Filtering.Handlers;

namespace HotChocolate.Data.Raven.Filtering;

public class RavenQueryableFilterProvider : QueryableFilterProvider
{
    public RavenQueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : base(configure)
    {
    }

    public RavenQueryableFilterProvider() { }

    protected override void Configure(IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler<RavenComparableInHandler>();
        descriptor.AddFieldHandler<RavenComparableNotInHandler>();
        descriptor.AddFieldHandler<RavenStringInHandler>();
        descriptor.AddFieldHandler<RavenStringNotInHandler>();
        descriptor.AddFieldHandler<RavenEnumInHandler>();
        descriptor.AddFieldHandler<RavenEnumNotInHandler>();
        descriptor.AddFieldHandler<RavenListAllOperationHandler>();
        descriptor.AddDefaultFieldHandlers();
    }

    /*
    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input) &&
            input is not IRavenQueryable<TEntityType> and not IRavenQueryable;
            */
}
