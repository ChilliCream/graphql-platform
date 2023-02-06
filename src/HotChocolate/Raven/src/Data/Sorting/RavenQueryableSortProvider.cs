using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Sorting;

public class RavenQueryableSortProvider : QueryableSortProvider
{
    public RavenQueryableSortProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure) : base(configure)
    {
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;

    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Func<IQueryable<TEntityType>, IQueryable<TEntityType>> sort)
        => input switch
        {
            IRavenQueryable<TEntityType> q => sort(q),
            IAsyncDocumentQuery<TEntityType> q => sort(q.ToQueryable()).ToAsyncDocumentQuery(),
            RavenAsyncDocumentQueryExecutable<TEntityType> ex =>
                new RavenAsyncDocumentQueryExecutable<TEntityType>(
                    sort(ex.Query.ToQueryable()).ToAsyncDocumentQuery()),
            _ => input
        };
}
