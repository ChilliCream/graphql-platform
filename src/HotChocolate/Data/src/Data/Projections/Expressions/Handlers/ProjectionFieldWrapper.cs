using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public class ProjectionFieldWrapper<T>
        : IProjectionFieldHandler<T>
        where T : IProjectionVisitorContext
    {
        private readonly ProjectionFieldHandler<T> _handler;
        private readonly IProjectionFieldInterceptor<T> _interceptor;

        public ProjectionFieldWrapper(
            ProjectionFieldHandler<T> handler,
            IProjectionFieldInterceptor<T> interceptor)
        {
            _handler = handler;
            _interceptor = interceptor;
        }

        public bool CanHandle(ISelection selection) =>
            _handler.CanHandle(selection);

        public IProjectionFieldHandler Wrap(IProjectionFieldInterceptor interceptor) =>
            _handler.Wrap(interceptor);

        public T OnBeforeEnter(T context, ISelection selection) =>
            _handler.OnBeforeEnter(context, selection);

        public bool TryHandleEnter(
            T context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            _interceptor.BeforeProjection(context, selection);
            return _handler.TryHandleEnter(context, selection, out action);
        }

        public T OnAfterEnter(T context, ISelection selection, ISelectionVisitorAction result) =>
            _handler.OnAfterEnter(context, selection, result);

        public T OnBeforeLeave(T context, ISelection selection) =>
            _handler.OnBeforeLeave(context, selection);

        public bool TryHandleLeave(
            T context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            _handler.TryHandleLeave(context, selection, out action);
            _interceptor.AfterProjection(context, selection);
            return action is not null;
        }

        public T OnAfterLeave(T context, ISelection selection, ISelectionVisitorAction result) =>
            _handler.OnAfterLeave(context, selection, result);
    }
}
