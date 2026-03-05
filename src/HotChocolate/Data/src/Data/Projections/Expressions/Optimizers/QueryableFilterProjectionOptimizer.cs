using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Data.Filters;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public sealed class QueryableFilterProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(Selection field)
        => field.Field.Member is { } && field.HasFilterFeature();

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        var resolverPipeline =
            selection.ResolverPipeline
                ?? context.CompileResolverPipeline(selection.Field, selection.SyntaxNodes[0].Node);

        static FieldDelegate WrappedPipeline(FieldDelegate next) =>
            ctx =>
            {
                ctx.LocalContextData = ctx.LocalContextData.SetItem(SkipFilteringKey, true);
                return next(ctx);
            };

        resolverPipeline = WrappedPipeline(resolverPipeline);

        context.SetResolver(selection, resolverPipeline);

        return selection;
    }

    public static QueryableFilterProjectionOptimizer Create(ProjectionProviderContext context) => new();
}
