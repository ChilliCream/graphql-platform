using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch;

public class QueryDefinition
{
    public QueryDefinition(
        IReadOnlyList<ISearchOperation> query,
        IReadOnlyList<ISearchOperation> filter)
    {
        Query = query;
        Filter = filter;
    }

    public IReadOnlyList<ISearchOperation> Query { get; }

    public IReadOnlyList<ISearchOperation> Filter { get; }
}
