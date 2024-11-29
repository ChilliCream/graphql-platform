using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

/// <summary>
/// The projection convention provides defaults for rewriter and providers filters.
/// </summary>
public interface IProjectionProvider : IConvention
{
    /// <summary>
    /// Creates a query builder that builds up the selection clause.
    /// </summary>
    /// <typeparam name="TEntityType">
    /// The entity type for which query builder shall be created.
    /// </typeparam>
    /// <returns>
    /// Returns a query builder that builds up the selection clause.
    /// </returns>
    IQueryBuilder CreateBuilder<TEntityType>();

    /// <summary>
    /// Rewrites a selection optimized for projection
    /// </summary>
    /// <param name="context">The context of the optimizer</param>
    /// <param name="selection">The selection to rewrite</param>
    /// <returns>
    /// Either a new rewritten selection or the same one if no rewriting was performed
    /// </returns>
    Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection);
}
