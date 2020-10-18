using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public abstract class ProjectionFieldHandler<T>
        : IProjectionFieldHandler<T>
        where T : IProjectionVisitorContext
    {
        public virtual IProjectionFieldHandler Wrap(IProjectionFieldInterceptor interceptor)
        {
            if (interceptor is IProjectionFieldInterceptor<T> interceptorOfT)
            {
                return new ProjectionFieldWrapper<T>(this, interceptorOfT);
            }

            return this;
        }

        public abstract bool CanHandle(ISelection selection);

        public virtual T OnBeforeEnter(T context, ISelection selection) => context;

        public abstract bool TryHandleEnter(
            T context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        public virtual T OnAfterEnter(
            T context,
            ISelection selection,
            ISelectionVisitorAction action) => context;

        public virtual T OnBeforeLeave(T context, ISelection selection) => context;

        public abstract bool TryHandleLeave(
            T context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        public virtual T OnAfterLeave(
            T context,
            ISelection selection,
            ISelectionVisitorAction action) => context;
    }
}
