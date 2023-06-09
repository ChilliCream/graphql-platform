using HotChocolate.Data.Sorting.Expressions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public sealed class QueryableSortProjectionOptimizer : QueryableKeysProjectionOptimizer
{
    protected override string ContextVisitArgumentKey =>
        QueryableSortProvider.ContextVisitSortArgumentKey;
    protected override string ContextArgumentNameKey =>
        QueryableSortProvider.ContextArgumentNameKey;
    protected override string SkipKey =>
        QueryableSortProvider.SkipSortingKey;
}
