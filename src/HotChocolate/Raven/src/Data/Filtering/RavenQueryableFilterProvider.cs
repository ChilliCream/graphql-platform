using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Raven.Filtering.Handlers;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

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

    protected override object? ApplyExpression<TEntityType>(
        object? input,
        Expression<Func<TEntityType, bool>> where)
        => input switch
        {
            IRavenQueryable<TEntityType> q => q.Where(where),
            IQueryable<TEntityType> q => q.Where(where),
            IEnumerable<TEntityType> e => e.AsQueryable().Where(where),
            IAsyncDocumentQuery<TEntityType> q => q
                .ToQueryable()
                .Where(where)
                .ToAsyncDocumentQuery(),
            RavenAsyncDocumentQueryExecutable<TEntityType> ex =>
                new RavenAsyncDocumentQueryExecutable<TEntityType>(
                    ex.Query.ToQueryable().Where(where).ToAsyncDocumentQuery()),
            _ => input
        };
}
