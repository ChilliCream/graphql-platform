namespace HotChocolate.Data.ElasticSearch;

internal static class ElasticSearchContextData
{
    public const string SortDefinitionKey =
        "HotChocolate.Data.ElasticSearch." + nameof(SortDefinitionKey);

    public const string FilterDefinitionKey =
        "HotChocolate.Data.ElasticSearch." + nameof(FilterDefinitionKey);

    public const string SkipSortingKey =
        "HotChocolate.Data.ElasticSearch." + nameof(SkipSortingKey);

    public const string SkipFilteringKey =
        "HotChocolate.Data.ElasticSearch." + nameof(SkipFilteringKey);
}
