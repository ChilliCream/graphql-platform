using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Resolvers;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Provides common extensions for <see cref="IElasticClient"/> to create search requests
/// </summary>
public static class ElasticSearchResolverContextExtensions
{
    /// <summary>
    /// Creates a new <see cref="SearchRequest"/> based on the current <paramref name="context"/>
    /// </summary>
    public static SearchRequest? CreateSearchRequest(
        this IElasticClient client,
        IResolverContext context)
    {
        if (!context.TryGetQueryFactory(out IElasticQueryFactory? factory))
        {
            return null;
        }

        BoolOperation? operation = factory.Create(context, ElasticSearchClient.From(client));

        return operation is not null
            ? new SearchRequest { Query = CreateQuery(operation) }
            : null;
    }

    /// <summary>
    /// Creates a new <see cref="SearchRequest{T}"/> based on the current
    /// <paramref name="context"/>
    /// </summary>
    public static SearchRequest<T>? CreateSearchRequest<T>(
        this IElasticClient client,
        IResolverContext context)
    {
        if (!context.TryGetQueryFactory(out IElasticQueryFactory? factory))
        {
            return null;
        }

        BoolOperation? operation = factory.Create(context, ElasticSearchClient.From(client));

        return operation is not null
            ? new SearchRequest<T> { Query = CreateQuery(operation) }
            : null;
    }

    /// <summary>
    /// Creates a new <see cref="SearchDescriptor{T}"/> based on the current
    /// <paramref name="context"/>
    /// </summary>
    public static SearchDescriptor<T>? CreateSearchDescriptor<T>(
        this IElasticClient client,
        IResolverContext context)
        where T : class
    {
        if (!context.TryGetQueryFactory(out IElasticQueryFactory? factory))
        {
            return null;
        }

        BoolOperation? operation = factory.Create(context, ElasticSearchClient.From(client));

        return operation is not null
            ? new SearchDescriptor<T>().Query(_ => CreateQuery(operation))
            : null;
    }

    private static bool TryGetQueryFactory(
        this IResolverContext context,
        [NotNullWhen(true)] out IElasticQueryFactory? factory)
    {
        if (context.LocalContextData.TryGetValue(nameof(IElasticQueryFactory), out object? value) &&
            value is IElasticQueryFactory f)
        {
            factory = f;
            return true;
        }

        factory = null;
        return false;
    }

    private static QueryContainer CreateQuery(ISearchOperation definition)
        => new((QueryBase)ElasticSearchOperationRewriter.Instance.Rewrite(definition));
}
