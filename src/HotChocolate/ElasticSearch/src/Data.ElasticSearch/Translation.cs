using System.Linq;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Resolvers;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

public static class ElasticSearchResolverContextExtensions
{
    public static SearchRequest? CreateSearchRequest(this IResolverContext context)
    {
        if (context.LocalContextData.TryGetValue(nameof(QueryDefinition), out object? value) &&
            value is QueryDefinition queryDefinition)
        {
            return new SearchRequest() {Query = CreateQuery(queryDefinition)};
        }

        return null;
    }

    public static SearchRequest<T>? CreateSearchRequest<T>(this IResolverContext context)
    {
        if (context.LocalContextData.TryGetValue(nameof(QueryDefinition), out object? value) &&
            value is QueryDefinition queryDefinition)
        {
            return new SearchRequest<T>() {Query = CreateQuery(queryDefinition)};
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
