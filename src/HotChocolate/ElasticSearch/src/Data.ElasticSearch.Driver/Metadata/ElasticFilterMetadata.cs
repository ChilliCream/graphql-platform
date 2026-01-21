namespace HotChocolate.Data.ElasticSearch.Filters;

/// <inheritdoc />
public class ElasticFilterMetadata : IElasticFilterMetadata
{
    /// <inheritdoc />
    public ElasticSearchOperationKind Kind { get; set; } = ElasticSearchOperationKind.Filter;

    /// <inheritdoc />
    public string? Path { get; set; }

    /// <summary>
    /// The default of the <see cref="ElasticFilterMetadata"/>
    /// </summary>
    public static IElasticFilterMetadata Default { get; } = new ElasticFilterMetadata();
}
