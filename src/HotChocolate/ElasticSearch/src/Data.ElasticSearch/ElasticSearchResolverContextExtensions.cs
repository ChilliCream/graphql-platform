using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.ElasticSearch.Sorting;
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
        var searchRequest = new SearchRequest
        {
            Query = new MatchAllQuery()
        };

        if (context.TryGetQueryFactory(out IElasticQueryFactory? factory))
        {
            BoolOperation? operation = factory.Create(context, ElasticSearchClient.From(client));
            if (operation is not null)
            {
                searchRequest.Query = CreateQuery(operation);
            }
        }

        if (context.TryGetSortFactory(out IElasticSortFactory? sortFactory))
        {
            var sorting = sortFactory
                .Create(context, ElasticSearchClient.From(client))
                .Select(sortOperation => new FieldSort
                {
                    Field = new Field(sortOperation.Path),
                    Order = sortOperation.Direction == ElasticSearchSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending
                })
                .OfType<ISort>()
                .ToList();

            searchRequest.Sort = sorting;
        }

        return searchRequest;
    }

    /// <summary>
    /// Creates a new <see cref="SearchRequest{T}"/> based on the current
    /// <paramref name="context"/>
    /// </summary>
    public static SearchRequest<T>? CreateSearchRequest<T>(
        this IElasticClient client,
        IResolverContext context)
    {
        var searchRequest = new SearchRequest<T>
        {
            Query = new MatchAllQuery()
        };

        if (context.TryGetQueryFactory(out IElasticQueryFactory? factory))
        {
            BoolOperation? operation = factory.Create(context, ElasticSearchClient.From(client));
            if (operation is not null)
            {
                searchRequest.Query = CreateQuery(operation);
            }
        }

        if (context.TryGetSortFactory(out IElasticSortFactory? sortFactory))
        {
            var sorting = sortFactory
                .Create(context, ElasticSearchClient.From(client))
                .Select(sortOperation => new FieldSort
                {
                    Field = new Field(sortOperation.Path),
                    Order = sortOperation.Direction == ElasticSearchSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending
                })
                .OfType<ISort>()
                .ToList();

            searchRequest.Sort = sorting;
        }

        return searchRequest;
    }

    /// <summary>
    /// Creates a new <see cref="SearchDescriptor{T}"/> based on the current
    /// <paramref name="context"/>
    /// </summary>
    public static SearchDescriptor<T> CreateSearchDescriptor<T>(
        this IElasticClient client,
        IResolverContext context)
        where T : class
    {

        var searchDescriptor = new SearchDescriptor<T>();
        searchDescriptor.Query(q => q.MatchAll());

        if (context.TryGetQueryFactory(out IElasticQueryFactory? factory))
        {
            BoolOperation? operation = factory.Create(context, ElasticSearchClient.From(client));
            if (operation is not null)
            {
                searchDescriptor.Query(_ => CreateQuery(operation));
            }
        }

        if (context.TryGetSortFactory(out IElasticSortFactory? sortFactory))
        {
            var sorting = sortFactory.Create(context, ElasticSearchClient.From(client));
            searchDescriptor.Sort(descriptor =>
            {
                foreach (var sortOperation in sorting)
                {
                    descriptor.Field(new Field(sortOperation.Path),
                        sortOperation.Direction == ElasticSearchSortDirection.Ascending
                            ? SortOrder.Ascending
                            : SortOrder.Descending);
                }

                return descriptor;
            });
        }

        return searchDescriptor;
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

    private static bool TryGetSortFactory(
        this IResolverContext context,
        [NotNullWhen(true)] out IElasticSortFactory? factory)
    {
        if (context.LocalContextData.TryGetValue(nameof(IElasticSortFactory), out object? value) &&
            value is IElasticSortFactory f)
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
