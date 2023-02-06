using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Raven.Filtering.Handlers;

namespace HotChocolate.Data.Raven.Filtering;

internal sealed class RavenQueryableFilterProvider : QueryableFilterProvider
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
        descriptor.AddFieldHandler<RavenStringNotContainsHandler>();
        descriptor.AddFieldHandler<RavenStringContainsHandler>();
        descriptor.AddDefaultFieldHandlers();
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;
}
