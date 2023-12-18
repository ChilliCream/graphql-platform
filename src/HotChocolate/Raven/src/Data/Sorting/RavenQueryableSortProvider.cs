using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Sorting;

/// <summary>
/// A <see cref="ISortProvider"/> for RavenDB
/// </summary>
internal sealed class RavenQueryableSortProvider : QueryableSortProvider
{
    public RavenQueryableSortProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure) : base(configure)
    {
    }

    /// <inheritdoc />
    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;

    /// <inheritdoc />
    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Func<IQueryable<TEntityType>, IQueryable<TEntityType>> sort)
        => input switch
        {
            IRavenQueryable<TEntityType> q => sort(q),
            IAsyncDocumentQuery<TEntityType> q => sort(q.ToQueryable()).ToAsyncDocumentQuery(),
            RavenAsyncDocumentQueryExecutable<TEntityType> q =>
                new RavenAsyncDocumentQueryExecutable<TEntityType>(
                    sort(q.Query.ToQueryable()).ToAsyncDocumentQuery()),
            _ => input,
        };
}
