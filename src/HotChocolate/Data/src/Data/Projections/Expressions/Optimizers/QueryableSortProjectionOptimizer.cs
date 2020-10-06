using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableSortProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.Field.ContextData.ContainsKey(QueryableSortProvider.ContextVisitSortArgumentKey) &&
            field.Field.ContextData.ContainsKey(QueryableSortProvider.ContextArgumentNameKey);

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            FieldDelegate resolverPipeline =
                context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);
            FieldMiddleware wrappedPipeline = next => ctx =>
            {
                ctx.LocalContextData.SetItem(QueryableSortProvider.SkipSortingKey, true);
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
