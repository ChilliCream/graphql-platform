using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.ExpressionNodes.Execution;

public sealed class ProjectionMiddleware
{
    private readonly FieldDelegate _next;

    public ProjectionMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        await _next(context).ConfigureAwait(false);

        // Ensure projections are only applied once
        if (context.LocalContextData.ContainsKey(ProjectionConstants.SkipProjection))
            return;
        context.LocalContextData = context.LocalContextData.SetItem(ProjectionConstants.SkipProjection, true);

        if (!context.ContextData.TryGet(ProjectionConstants.CacheLease, out var cacheLease))
        {
            if (!context.ContextData.TryGet(ProjectionConstants.TreeCache, out var treeCache))
                throw new InvalidOperationException();

            // TODO: get from some system or whatever.
            var variables = new ReadOnlyCollection<(Identifier, Variable)>(null!);

            cacheLease = treeCache.LeaseCache(variables);

            context.ContextData.Set(ProjectionConstants.CacheLease, cacheLease);
        }

        var root = cacheLease.GetRootExpression();

        if (context.Result is IQueryable queryable)
            context.Result = queryable.SelectT(root);

        else if (context.Result is IEnumerable enumerable)
            context.Result = enumerable.SelectT(root.Compile());
    }
}

public static class ProjectionConstants
{
    public static readonly StateKey<BorrowedProjectionExpressionCache> CacheLease = StateKey.Create<BorrowedProjectionExpressionCache>();
    internal static readonly StateKey<ProjectionExpressionCompiler> TreeCache = StateKey.Create<ProjectionExpressionCompiler>();
    public static readonly StateFlagKey SkipProjection = StateFlagKey.Create("SkipProjection");
}
