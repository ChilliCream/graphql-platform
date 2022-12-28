using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Execution;

public interface IElasticSearchExecutable : IExecutable
{
    string GetName(IFilterField field);

    string GetName(ISortField field);

    IElasticSearchExecutable WithFiltering(BoolOperation filter);

    IElasticSearchExecutable WithSorting(IReadOnlyList<ElasticSearchSortOperation> sorting);

    IElasticSearchExecutable WitPagination(int take, int skip);
}
