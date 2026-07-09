using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Raven.Filtering.Handlers;
using Microsoft.Extensions.DependencyInjection;
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

    [ActivatorUtilitiesConstructor]
    public RavenQueryableFilterProvider() { }

    protected override void Configure(IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler(RavenComparableInHandler.Create);
        descriptor.AddFieldHandler(RavenComparableNotInHandler.Create);
        descriptor.AddFieldHandler(RavenStringInHandler.Create);
        descriptor.AddFieldHandler(RavenStringNotInHandler.Create);
        descriptor.AddFieldHandler(RavenEnumInHandler.Create);
        descriptor.AddFieldHandler(RavenEnumNotInHandler.Create);
        descriptor.AddFieldHandler(RavenStringNotContainsHandler.Create);
        descriptor.AddFieldHandler(RavenStringContainsHandler.Create);
        descriptor.AddDefaultFieldHandlers();
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;

    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, bool>> where)
        => input switch
        {
            IRavenQueryable<TEntityType> q => q.Where(where),
            IAsyncDocumentQuery<TEntityType> q => q
                .ToQueryable()
                .Where(where)
                .ToAsyncDocumentQuery(),
            RavenAsyncDocumentQueryExecutable<TEntityType> q =>
                new RavenAsyncDocumentQueryExecutable<TEntityType>(
                    q.Query.ToQueryable().Where(where).ToAsyncDocumentQuery()),
            _ => input
        };
}
