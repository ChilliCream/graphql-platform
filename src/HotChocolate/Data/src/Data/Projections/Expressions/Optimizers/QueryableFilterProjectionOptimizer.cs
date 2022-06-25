using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Projections.Handlers;

public class QueryableFilterProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection field) =>
        field.Field.Member is { } &&
        field.Field.ContextData.ContainsKey(ContextVisitFilterArgumentKey) &&
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
                ctx.LocalContextData = ctx.LocalContextData.SetItem(
                    SkipFilteringKey,
                    true);

                return next(ctx);
            };

        resolverPipeline = WrappedPipeline(resolverPipeline);

        var compiledSelection = new Selection(
            selection.Id,
            context.Type,
            selection.Field,
            selection.Field.Type,
            selection.SyntaxNode,
            selection.ResponseName,
            SelectionExecutionStrategy.Default,
            selection.Arguments,
            // TODO I think i need to have access to the include conditions here
            resolverPipeline:resolverPipeline);

        context.ReplaceSelection(compiledSelection.ResponseName, compiledSelection);
        return compiledSelection;
    }
}
