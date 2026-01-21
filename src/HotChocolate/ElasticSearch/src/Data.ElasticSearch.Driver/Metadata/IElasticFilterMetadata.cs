using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Metadata for elastic search / open search that can be annotated on the
/// <see cref="IFilterField"/>
/// </summary>
public interface IElasticFilterMetadata : IFilterMetadata
{
    /// <summary>
    /// The <see cref="ElasticSearchOperationKind"/> of this field
    /// </summary>
    ElasticSearchOperationKind Kind { get; }

    /// <summary>
    /// The path override for this field
    /// </summary>
    string? Path { get; }
}
