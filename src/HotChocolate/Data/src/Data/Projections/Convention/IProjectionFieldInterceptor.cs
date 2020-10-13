using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldInterceptor
    {
        /// <summary>
        /// Tests if this interceptor can handle a selection If it can handle the selection it
        /// will be attached to the compiled selection set on the
        /// type <see cref="ProjectionSelection"/>
        /// </summary>
        /// <param name="selection">The selection to test for</param>
        /// <returns>Returns true if the selection can be handled</returns>
        bool CanHandle(ISelection selection);
    }
}
