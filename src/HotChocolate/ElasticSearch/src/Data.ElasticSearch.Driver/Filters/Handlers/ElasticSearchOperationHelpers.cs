using System;

namespace HotChocolate.Data.ElasticSearch.Filters;

public static class ElasticSearchOperationHelpers
{
    public static ISearchOperation Negate(ISearchOperation operation) 
        => new BoolOperation(
            Array.Empty<ISearchOperation>(),
            Array.Empty<ISearchOperation>(),
            new[]{operation});
}
