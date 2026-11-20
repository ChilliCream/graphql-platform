using System.Linq.Expressions;
using System.Text.Json;
using Elastic.Esql.Core;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch.Filtering;

internal sealed class ElasticSearchQueryableFilterProvider : QueryableFilterProvider
{
    private readonly JsonNamingPolicy? _namingPolicy = JsonNamingPolicy.CamelCase;

    private readonly JsonSerializerOptions? _options;

    public ElasticSearchQueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : base(configure)
    {
    }

    [ActivatorUtilitiesConstructor]
    public ElasticSearchQueryableFilterProvider() { }

    public ElasticSearchQueryableFilterProvider(JsonNamingPolicy? namingPolicy)
    {
        _namingPolicy = namingPolicy;
    }

    public ElasticSearchQueryableFilterProvider(JsonSerializerOptions options)
    {
        _namingPolicy = options.PropertyNamingPolicy;
        _options = options;
    }

    protected override void Configure(IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        => descriptor.AddDefaultFieldHandlers();

    // other sources, such as a list in memory or Entity Framework, are filtered as QueryableFilterProvider does.
    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => !IsElasticSearchQuery<TEntityType>(input) && base.IsInMemoryQuery<TEntityType>(input);

    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, bool>> where)
    {
        if (!IsElasticSearchQuery<TEntityType>(input))
        {
            return base.ApplyToResult(input, where);
        }

        var rewriter = new ElasticSearchFilterRewriter(_namingPolicy, _options);
        var rewritten = (Expression<Func<TEntityType, bool>>)rewriter.Visit(where);
        return base.ApplyToResult(input, rewritten);
    }

    private static bool IsElasticSearchQuery<TEntityType>(object? input)
    {
        var query = input switch
        {
            IQueryableExecutable<TEntityType> executable => executable.Source,
            IQueryable<TEntityType> queryable => queryable,
            _ => null
        };

        return query?.Provider is EsqlQueryProvider;
    }
}
