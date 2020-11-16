using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionOptimizer
    {
        /// <summary>
        /// Tests if this optimizer can handle a selection If it can handle the selection it
        /// will be attached to the compiled selection set on the
        /// type <see cref="ProjectionSelection"/>
        /// </summary>
        /// <param name="selection">The selection to test for</param>
        /// <returns>Returns true if the selection can be handled</returns>
        bool CanHandle(ISelection selection);

        /// <summary>
        /// Rewrites a selection. In case nothing is rewritten, the <paramref name="selection"/>
        /// is returned
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        /// <returns>
        /// Returns either the original <paramref name="selection"/> or a rewritten version of it
        /// </returns>
        Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection);
    }
}
