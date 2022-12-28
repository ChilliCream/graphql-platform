using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Execution;

public interface IElasticSearchExecutable : IExecutable
{
    IElasticSearchExecutable WithFiltering(BoolOperation filter);

    IElasticSearchExecutable WithSorting(IReadOnlyList<ElasticSearchSortOperation> sorting);

    IElasticSearchExecutable WitPagination(int take, int skip);
}

public interface IElasticSearchExecutable<T> : IElasticSearchExecutable, IExecutable<T>
{
    Task<IList<T>> ExecuteAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}
