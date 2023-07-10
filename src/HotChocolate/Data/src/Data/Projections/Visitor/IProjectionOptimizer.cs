using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections;

public interface IProjectionOptimizer
{
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
        SelectionSetOptimizerContext context,
        Selection selection);
}
