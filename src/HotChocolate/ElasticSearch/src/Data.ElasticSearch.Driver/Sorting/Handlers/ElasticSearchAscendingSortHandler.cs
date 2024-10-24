using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Sorting.Handlers;

public class ElasticSearchAscendingSortHandler : ElasticSearchSortOperationHandlerBase
{
    public ElasticSearchAscendingSortHandler()
        : base(DefaultSortOperations.Ascending, ElasticSearchSortDirection.Ascending)
    {
    }
}
