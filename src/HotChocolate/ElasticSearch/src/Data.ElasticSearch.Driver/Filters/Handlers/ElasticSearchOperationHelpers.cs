namespace HotChocolate.Data.ElasticSearch.Filters;

public static class ElasticSearchOperationHelpers
{
    public static ISearchOperation Negate(ISearchOperation operation)
        => BoolOperation.Create(mustNot: new[] { operation });
}
