using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

namespace HotChocolate.Data.Projections.Handlers;

public sealed class QueryableSortProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection field) =>
        field.Field.Member is { } &&
        field.Field.ContextData.ContainsKey(ContextVisitSortArgumentKey) &&
        field.Field.ContextData.ContainsKey(ContextArgumentNameKey);

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        var resolverPipeline =
            selection.ResolverPipeline ??
            context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);

        static FieldDelegate WrappedPipeline(FieldDelegate next) =>
            ctx =>
            {
                ctx.LocalContextData = ctx.LocalContextData.SetItem(SkipSortingKey, true);
                return next(ctx);
            };

        resolverPipeline = WrappedPipeline(resolverPipeline);

        context.SetResolver(selection, resolverPipeline);

        return selection;
    }
}
