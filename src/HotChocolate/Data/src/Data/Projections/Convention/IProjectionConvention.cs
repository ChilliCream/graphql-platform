using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// The projection convention provides defaults for rewriter and providers filters.
    /// </summary>
    public interface IProjectionConvention : IConvention
    {
        /// <summary>
        /// Creates a middleware that represents the filter execution logic
        /// for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntityType">
        /// The entity type for which an filter executor shall be created.
        /// </typeparam>
        /// <returns>
        /// Returns a field middleware which represents the filter execution logic
        /// for the specified entity type.
        /// </returns>
        FieldMiddleware CreateExecutor<TEntityType>();

        Selection RewriteSelection(Selection selection);
    }
}
