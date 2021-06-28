using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Handlers
{
    /// <summary>
    /// This wrapper is used to combined two interceptors and create a chain
    /// </summary>
    internal class ProjectionInterceptorCombinator<T>
        : IProjectionFieldInterceptor<T>
        where T : IProjectionVisitorContext
    {
        private readonly IProjectionFieldInterceptor _current;
        private readonly IProjectionFieldInterceptor _next;

        public ProjectionInterceptorCombinator(
            IProjectionFieldInterceptor current,
            IProjectionFieldInterceptor next)
        {
            _current = current;
            _next = next;
        }

        public bool CanHandle(ISelection selection) => true;

        public void BeforeProjection(
            T context,
            ISelection selection)
        {
            if (_current is IProjectionFieldInterceptor<T> currentHandler)
            {
                currentHandler.BeforeProjection(context, selection);
            }

            if (_next is IProjectionFieldInterceptor<T> nextHandler)
            {
                nextHandler.BeforeProjection(context, selection);
            }
        }

        public void AfterProjection(T context, ISelection selection)
        {
            if (_next is IProjectionFieldInterceptor<T> nextHandler)
            {
                nextHandler.AfterProjection(context, selection);
            }

            if (_current is IProjectionFieldInterceptor<T> currentHandler)
            {
                currentHandler.AfterProjection(context, selection);
            }
        }
    }
}
