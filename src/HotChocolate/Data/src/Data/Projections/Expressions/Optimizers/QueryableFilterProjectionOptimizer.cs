using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableFilterProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.Field.Member is {} &&
            field.Field.ContextData.ContainsKey(ContextVisitFilterArgumentKey) &&
            field.Field.ContextData.ContainsKey(ContextArgumentNameKey);

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            FieldDelegate resolverPipeline =
                context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);
            FieldMiddleware wrappedPipeline = next => ctx =>
            {
                ctx.LocalContextData = ctx.LocalContextData.SetItem(
                    SkipFilteringKey,
                    true);

                return next(ctx);
            };
            resolverPipeline = wrappedPipeline(resolverPipeline);

            var compiledSelection = new Selection(
                context.Type,
                selection.Field,
                selection.SyntaxNode,
                resolverPipeline,
                arguments: selection.Arguments,
                internalSelection: false);

            context.Fields[compiledSelection.ResponseName] = compiledSelection;
            return compiledSelection;
        }
    }
}
