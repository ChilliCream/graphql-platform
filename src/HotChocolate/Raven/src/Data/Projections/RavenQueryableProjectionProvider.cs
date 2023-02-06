using System.Linq.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Projections;

public class RavenQueryableProjectionProvider : QueryableProjectionProvider
{
    protected override void Configure(IProjectionProviderDescriptor descriptor)
    {
        descriptor.AddDefaults();
    }

    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, TEntityType>> projection)
        => input switch
        {
            IRavenQueryable<TEntityType> q => q.Select(projection),
            IAsyncDocumentQuery<TEntityType> q => q
                .ToQueryable()
                .Select(projection)
                .ToAsyncDocumentQuery(),
            RavenAsyncDocumentQueryExecutable<TEntityType> ex =>
                new RavenAsyncDocumentQueryExecutable<TEntityType>(
                    ex.Query.ToQueryable().Select(projection).ToAsyncDocumentQuery()),
            _ => input
        };

    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;
}
