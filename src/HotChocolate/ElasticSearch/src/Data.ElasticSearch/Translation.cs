using System.Linq;
using System.Reflection;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Resolvers;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

internal class ElasticSearchClient : IAbstractElasticClient
{
    private readonly IElasticClient _client;

    public ElasticSearchClient(IElasticClient client)
    {
        _client = client;
    }

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

    public static ElasticSearchClient From(IElasticClient client) => new(client);
}

public static class ElasticSearchResolverContextExtensions
{
    public static SearchRequest? CreateSearchRequest(
        this IElasticClient client,
        IResolverContext context)
    {
        if (context.LocalContextData.TryGetValue(nameof(IElasticQueryFactory), out object? value) &&
            value is IElasticQueryFactory translator)
        {
            QueryDefinition? queryDefinition =
                translator.Create(context, ElasticSearchClient.From(client));
            if (queryDefinition is not null)
            {
                return new SearchRequest() {Query = CreateQuery(queryDefinition)};
            }
        }

        return null;
    }

    public static SearchRequest<T>? CreateSearchRequest<T>(
        this IElasticClient client,
        IResolverContext context)
    {
        if (context.LocalContextData.TryGetValue(nameof(IElasticQueryFactory), out object? value) &&
            value is IElasticQueryFactory translator)
        {
            QueryDefinition? queryDefinition =
                translator.Create(context, ElasticSearchClient.From(client));
            if (queryDefinition is not null)
            {
                return new SearchRequest<T>() {Query = CreateQuery(queryDefinition)};
            }
        }

        return null;
    }

    public static SearchDescriptor<T>? CreateSearchDescriptor<T>(
        this IElasticClient client,
        IResolverContext context)
        where T : class
    {
        if (context.LocalContextData.TryGetValue(nameof(IElasticQueryFactory), out object? value) &&
            value is IElasticQueryFactory translator)
        {
            QueryDefinition? queryDefinition =
                translator.Create(context, ElasticSearchClient.From(client));
            if (queryDefinition is not null)
            {
                return new SearchDescriptor<T>().Query(_ => CreateQuery(queryDefinition));
            }
        }

        return null;
    }

    private static QueryContainer CreateQuery(QueryDefinition definition)
    {
        if (definition.Query.Count == 1 &&
            definition.Filter.Count == 0 &&
            definition.Query[0] is BoolOperation &&
            ElasticSearchOperationRewriter.Instance
                .Rewrite(definition.Query[0]) is QueryBase reduced)
        {
            return new QueryContainer(reduced);
        }

        return new BoolQuery()
        {
            Must = definition.Query
                .Select(ElasticSearchOperationRewriter.Instance.Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x))
                .ToArray(),
            Filter = definition.Filter
                .Select(ElasticSearchOperationRewriter.Instance.Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x))
                .ToArray(),
        };
    }
}

public class ElasticSearchOperationRewriter : SearchOperationRewriter<IQuery>
{
    protected override IQuery Rewrite(BoolOperation operation)
    {
        return new BoolQuery()
        {
            Should = operation.Should
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x)),
            Must = operation.Must
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x)),
        };
    }

    protected override IQuery Rewrite(MatchOperation operation)
    {
        return new MatchQuery() {Field = operation.Path, Query = operation.Value};
    }

    protected override IQuery Rewrite(RangeOperation operation)
    {
        throw new System.NotImplementedException();
    }

    protected override IQuery Rewrite(TermOperation operation)
    {
        throw new System.NotImplementedException();
    }

    public static ElasticSearchOperationRewriter Instance { get; } = new();
}
