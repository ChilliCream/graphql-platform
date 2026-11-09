using System.Linq.Expressions;
using System.Text.Json;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch.Filtering;

internal sealed class ElasticSearchQueryableFilterProvider : QueryableFilterProvider
{
    private readonly JsonNamingPolicy? _namingPolicy = JsonNamingPolicy.CamelCase;

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

    protected override void Configure(IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        => descriptor.AddDefaultFieldHandlers();

    protected override bool IsInMemoryQuery<TEntityType>(object? input) => false;

    protected override object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, bool>> where)
    {
        var rewriter = new ElasticSearchFilterRewriter(_namingPolicy);
        var rewritten = (Expression<Func<TEntityType, bool>>)rewriter.Visit(where);
        return base.ApplyToResult(input, rewritten);
    }
}
