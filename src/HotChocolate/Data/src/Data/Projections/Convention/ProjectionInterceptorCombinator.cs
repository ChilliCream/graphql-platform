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
        private readonly IProjectionFieldInterceptor _first;
        private readonly IProjectionFieldInterceptor _second;

        public ProjectionInterceptorCombinator(
            IProjectionFieldInterceptor first,
            IProjectionFieldInterceptor second)
        {
            _first = first;
            _second = second;
        }

        public bool CanHandle(ISelection selection) => true;

        public void BeforeProjection(
            T context,
            ISelection selection)
        {
            if (_first is IProjectionFieldInterceptor<T> firstHandler)
            {
                firstHandler.BeforeProjection(context, selection);
            }

            if (_second is IProjectionFieldInterceptor<T> secondHandler)
            {
                secondHandler.BeforeProjection(context, selection);
            }
        }

        public void AfterProjection(T context, ISelection selection)
        {
            if (_first is IProjectionFieldInterceptor<T> firstHandler)
            {
                firstHandler.AfterProjection(context, selection);
            }

            if (_second is IProjectionFieldInterceptor<T> secondHandler)
            {
                secondHandler.AfterProjection(context, selection);
            }
        }
    }
}
