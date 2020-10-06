using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionOptimizer
    {
        bool CanHandle(ISelection selection);

        Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection);
    }
}
