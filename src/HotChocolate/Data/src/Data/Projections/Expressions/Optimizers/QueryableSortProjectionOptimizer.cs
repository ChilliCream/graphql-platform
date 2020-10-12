using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableSortProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.Field.Member is {} &&
            field.Field.ContextData.ContainsKey(ContextVisitSortArgumentKey) &&
            field.Field.ContextData.ContainsKey(ContextArgumentNameKey);

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            FieldDelegate resolverPipeline =
                context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);

            static FieldDelegate WrappedPipeline(FieldDelegate next) =>
                ctx =>
                {
                    ctx.LocalContextData = ctx.LocalContextData.SetItem(SkipSortingKey, true);
                    return next(ctx);
                };

            resolverPipeline = WrappedPipeline(resolverPipeline);

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
