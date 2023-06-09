using HotChocolate.Data.Filters.Expressions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public sealed class QueryableFilterProjectionOptimizer : QueryableKeysProjectionOptimizer
{
    protected override string ContextVisitArgumentKey =>
        QueryableFilterProvider.ContextVisitFilterArgumentKey;
    protected override string ContextArgumentNameKey =>
        QueryableFilterProvider.ContextArgumentNameKey;
    protected override string SkipKey =>
        QueryableFilterProvider.SkipFilteringKey;
}
