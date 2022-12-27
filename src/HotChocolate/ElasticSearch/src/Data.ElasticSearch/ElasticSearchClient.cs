using System.Reflection;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
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
        IElasticFilterMetadata metadata = field.GetElasticMetadata();
        if (metadata.Name is { })
        {
            return metadata.Name;
        }

        if (field.Member is PropertyInfo propertyInfo)
        {
            return _client.Infer.Field(new Field(propertyInfo));
        }

        if (field.Member is { Name: { } memberName })
        {
            return memberName;
        }

        return field.Name;
    }

    /// <inheritdoc />
    public string GetName(ISortField field)
    {
        if (field.Member is PropertyInfo propertyInfo)
        {
            return AddKeywordSuffix(_client.Infer.Field(new Field(propertyInfo)));
        }

        if (field.Member is { Name: { } memberName })
        {
            return AddKeywordSuffix(memberName);
        }

        return AddKeywordSuffix(field.Name);
    }

    private string AddKeywordSuffix(string val) => $"{val}.keyword";

    /// <summary>
    /// Creates a new instance of <see cref="ElasticSearchClient"/> based on the
    /// <see cref="IElasticClient"/>
    /// </summary>
    public static ElasticSearchClient From(IElasticClient client) => new(client);
}
