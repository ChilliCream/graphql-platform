using System.Reflection;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// A thing wrapper around <see cref="ElasticClient"/> that is known by the data driver
/// </summary>
internal class ElasticSearchClient : IAbstractElasticClient
{
    private readonly IElasticClient _client;

    public ElasticSearchClient(IElasticClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public string GetName(IFilterField field)
    {
        // TODO add field override
        // TODO add expression
        if (field.Member is PropertyInfo propertyInfo)
        {
            return _client.Infer.Field(new Field(propertyInfo));
        }

        string fieldName = field.Name;

        if (field.Member is { } p)
        {
            fieldName = p.Name;
        }

        return fieldName;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ElasticSearchClient"/> based on the
    /// <see cref="IElasticClient"/>
    /// </summary>
    public static ElasticSearchClient From(IElasticClient client) => new(client);
}
