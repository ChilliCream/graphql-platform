using System.Linq.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Projections;

/// <summary>
/// A <see cref="IProjectionProvider"/> for RavenDB
/// </summary>
internal sealed class RavenQueryableProjectionProvider : QueryableProjectionProvider
{
    /// <inheritdoc />
    protected override void Configure(IProjectionProviderDescriptor descriptor)
    {
        descriptor.AddDefaults();
    }

    /// <inheritdoc />
    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, TEntityType>> projection)
        => input switch
        {
            IRavenQueryable<TEntityType> q => q
                .Customize(x => x.NoTracking())
                .Select(projection),
            IAsyncDocumentQuery<TEntityType> q => q
                .NoTracking()
                .ToQueryable()
                .Select(projection)
                .ToAsyncDocumentQuery(),
            RavenAsyncDocumentQueryExecutable<TEntityType> q =>
                new RavenAsyncDocumentQueryExecutable<TEntityType>(
                    q.Query.NoTracking().ToQueryable().Select(projection).ToAsyncDocumentQuery()),
            _ => input,
        };

    /// <inheritdoc />
    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;
}
