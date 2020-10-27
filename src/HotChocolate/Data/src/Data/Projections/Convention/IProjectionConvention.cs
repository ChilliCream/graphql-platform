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
        /// Creates a new selection optimizer for this projection convention.
        /// </summary>
        /// <returns>
        /// Returns the selection optimizer for this projection convention.
        /// </returns>
        ISelectionOptimizer CreateOptimizer();
    }
}
