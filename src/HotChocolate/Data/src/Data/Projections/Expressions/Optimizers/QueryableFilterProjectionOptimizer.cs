using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableFilterProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.Field.ContextData.ContainsKey(
                QueryableFilterProvider.ContextVisitFilterArgumentKey) &&
            field.Field.ContextData.ContainsKey(QueryableFilterProvider.ContextArgumentNameKey);

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            FieldDelegate resolverPipeline =
                context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);
            FieldMiddleware wrappedPipeline = next => ctx =>
            {
                ctx.LocalContextData = ctx.LocalContextData.SetItem(
                    QueryableFilterProvider.SkipFilteringKey,
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
