using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Provides extension for <see cref="IFilterField"/> for <see cref="IElasticFilterMetadata"/>
/// </summary>
public static class ElasticSearchFilterFieldExtensions
{
    /// <summary>
    /// Returns the <see cref="IElasticFilterMetadata"/> for <paramref name="field"/>
    /// </summary>
    public static IElasticFilterMetadata GetElasticMetadata(this IFilterField field)
        => field.Metadata as ElasticFilterMetadata ?? ElasticFilterMetadata.Default;
}
