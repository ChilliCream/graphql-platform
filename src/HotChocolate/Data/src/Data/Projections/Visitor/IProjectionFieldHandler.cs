using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// A handler that can intersect a <see cref="ISelection"/> and optimize the selection set for
    /// projections.
    /// </summary>
    public interface IProjectionFieldHandler
    {
        /// <summary>
        /// Tests if this field handle can handle a selection. If it can handle the selection it
        /// will be attached to the compiled selection set on the
        /// type <see cref="ProjectionSelection"/>
        /// </summary>
        /// <param name="selection">The selection to test for</param>
        /// <returns>Returns true if the selection can be handled</returns>
        bool CanHandle(ISelection selection);

        /// <summary>
        /// Wrapped this field handler with a type interceptor
        /// </summary>
        /// <param name="interceptor">
        /// The interceptor that this handler should be wrapped with
        /// </param>
        /// <returns>The wrapped handler</returns>
        IProjectionFieldHandler Wrap(IProjectionFieldInterceptor interceptor);
    }
}
