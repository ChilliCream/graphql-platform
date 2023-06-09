using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Handlers;

public abstract class QueryableKeysProjectionOptimizer : IProjectionOptimizer
{
    protected abstract string ContextVisitArgumentKey { get; }
    protected abstract string ContextArgumentNameKey { get; }
    protected abstract string SkipKey { get; }

    private bool CanHandle(ISelection field) =>
        field.Field.Member is { } &&
        field.Field.ContextData.ContainsKey(ContextVisitArgumentKey) &&
        field.Field.ContextData.ContainsKey(ContextArgumentNameKey);

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (!CanHandle(selection))
        {
            return selection;
        }

        if (selection.Strategy is SelectionExecutionStrategy.Pure)
        {
            return selection;
        }

        FieldDelegate resolverPipeline =
            selection.ResolverPipeline ??
            context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);

        FieldDelegate WrappedPipeline(FieldDelegate next) =>
            ctx =>
            {
                ctx.LocalContextData = ctx.LocalContextData.SetItem(
                    SkipKey,
                    true);
                return next(ctx);
            };

        resolverPipeline = WrappedPipeline(resolverPipeline);

        context.SetResolver(selection, resolverPipeline);

        return selection;
    }
}
