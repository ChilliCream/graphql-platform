using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Sorting.Handlers;

public class ElasticSearchDescendingSortHandler : ElasticSearchSortOperationHandlerBase
{
    /// <inheritdoc />
    public ElasticSearchDescendingSortHandler() : base(
        DefaultSortOperations.Descending,
        ElasticSearchSortDirection.Descending)
    {
    }
}
