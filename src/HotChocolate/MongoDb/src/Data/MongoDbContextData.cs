namespace HotChocolate.Data.MongoDb;

public static class MongoDbContextData
{
    public const string SortDefinitionKey = "HotChocolate.Data.MongoDb." + nameof(SortDefinitionKey);

    internal const string SkipSortingKey = "HotChocolate.Data.MongoDb." + nameof(SkipSortingKey);

    public const string FilterDefinitionKey = "HotChocolate.Data.MongoDb." + nameof(FilterDefinitionKey);

    internal const string SkipFilteringKey = "HotChocolate.Data.MongoDb." + nameof(SkipFilteringKey);

    public const string ProjectionDefinitionKey = "HotChocolate.Data.MongoDb." + nameof(ProjectionDefinitionKey);

    internal const string SkipProjectionKey = "HotChocolate.Data.MongoDb." + nameof(SkipProjectionKey);
}
