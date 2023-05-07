namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// A fluent configuration API for the metadata of a elastic search filter field..
/// </summary>
public interface IElasticFilterMetadataDescriptor
{
    /// <summary>
    /// Overrides the name of the field.
    /// <example>
    /// <code lang="csharp">
    /// descriptor
    ///      .Field(x => x.Name)
    ///      .ConfigureElastic(x => x.Path("thisIsTheOverride"))
    /// </code>
    /// <code lang="json">
    /// {
    ///    "match": {
    ///       "deep.thisIsTheOverride": {
    ///             "query": "The value of the field"
    ///       }
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IElasticFilterMetadataDescriptor Path(string name);

    /// <summary>
    /// Sets the <see cref="ElasticSearchOperationKind"/>. Decides if the field
    /// should count as a "query" or a "filter"
    /// </summary>
    IElasticFilterMetadataDescriptor Kind(ElasticSearchOperationKind kind);

    /// <summary>
    /// Treat this field as a "filter"
    /// </summary>
    IElasticFilterMetadataDescriptor AsFilter();

    /// <summary>
    /// Treat this field as a "query"
    /// </summary>
    IElasticFilterMetadataDescriptor AsQuery();
}
