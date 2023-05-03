using System.Collections;
using System.Text.Json;
using Elasticsearch.Net;
using HotChocolate.Data.ElasticSearch.Execution;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

public sealed class NestExecutable<T> : ElasticSearchExecutable<T> where T : class
{
    private readonly IElasticClient _elasticClient;

    /// <inheritdoc />
    public override object Source => this;

    public NestExecutable(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    /// <inheritdoc />
    public override async ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        searchDescriptor.Size = 1;
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return result.Hits.Select(hit => hit.Source).FirstOrDefault();
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return result.Hits.Select(hit => hit.Source).ToList();
    }

    /// <inheritdoc />
    public override async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        searchDescriptor.Size = 0;
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return (int)result.Total;
    }

    /// <inheritdoc />
    public override string Print()
    {
        MemoryStream stream = new();
        SerializableData<SearchRequest> data = new(CreateQuery());
        data.Write(stream, new ConnectionSettings(new InMemoryConnection()));
        stream.Position = 0;
        JsonDocument deserialized = JsonDocument.Parse(stream);
        JsonSerializerOptions options = new() { WriteIndented = true };
        return JsonSerializer.Serialize(deserialized, options);
    }

    /// <inheritdoc />
    public override async ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        searchDescriptor.Size = 1;
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return result.Hits.Select(hit => hit.Source).FirstOrDefault();
    }

    /// <inheritdoc />
    public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(cancellationToken);
        return result.ToList();
    }

    private SearchRequest<T> CreateQuery()
    {
        var searchRequest = new SearchRequest<T>
        {
            Query = new MatchAllQuery()
        };

        if (Filters is ISearchOperation filters)
        {
            var query = (QueryBase)ElasticSearchOperationRewriter.Instance.Rewrite(filters);
            searchRequest.Query = query;
        }

        if (Sorting is {Count: > 0} sortOperations)
        {
            searchRequest.Sort = sortOperations
                .Select(sortOperation => new FieldSort
                {
                    Field = new Field(sortOperation.Path.GetKeywordPath()),
                    Order = sortOperation.Direction == ElasticSearchSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending
                })
                .OfType<ISort>()
                .ToList();
        }

        if (Take is not null)
        {
            searchRequest.Size = Take;
        }

        if (Skip is not null)
        {
            searchRequest.From = Skip;
        }

        return searchRequest;
    }
}

