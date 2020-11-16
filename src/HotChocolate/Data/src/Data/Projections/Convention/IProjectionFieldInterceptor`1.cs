using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldInterceptor<in TContext>
        : IProjectionFieldInterceptor
        where TContext : IProjectionVisitorContext
    {
        /// <summary>
        /// This method is called before the enter and leave methods of a
        /// <see cref="IProjectionFieldHandler{TContext}"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        void BeforeProjection(
            TContext context,
            ISelection selection);

        /// <summary>
        /// This method is called after the enter and leave methods of a
        /// <see cref="IProjectionFieldHandler{TContext}"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        void AfterProjection(
            TContext context,
            ISelection selection);
    }
}
