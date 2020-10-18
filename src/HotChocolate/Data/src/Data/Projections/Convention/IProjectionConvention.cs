using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// The projection convention provides defaults for projections projections.
    /// </summary>
    public interface IProjectionConvention : IConvention
    {
        /// <summary>
        /// Creates a middleware that represents the projection execution logic
        /// for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntityType">
        /// The entity type for which an projection executor shall be created.
        /// </typeparam>
        /// <returns>
        /// Returns a field middleware which represents the projection execution logic
        /// for the specified entity type.
        /// </returns>
        FieldMiddleware CreateExecutor<TEntityType>();

        /// <summary>
        /// Rewrites a selection optimized for projection
        /// </summary>
        /// <param name="context">The context of the optimizer</param>
        /// <param name="selection">The selection to rewrite</param>
        /// <returns>
        /// Either a new rewritten selection or the same one if no rewriting was performed
        /// </returns>
        Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection);
    }
}
