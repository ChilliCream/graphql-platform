using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Represents an abstract wrapper around either a elastic search
/// or open search client
/// </summary>
public interface IAbstractElasticClient
{
    /// <summary>
    /// Gets the name of the field as a string value
    /// </summary>
    string GetName(IFilterField field);

    /// <summary>
    /// Gets the name of the field as a string value
    /// </summary>
    string GetName(ISortField field);
}
