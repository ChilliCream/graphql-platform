using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    /// <summary>
    /// A handler that can intersect a <see cref="ISelection"/> and optimize the selection set for
    /// projections.
    /// </summary>
    public abstract class ProjectionFieldHandler<T>
        : IProjectionFieldHandler<T>
        where T : IProjectionVisitorContext
    {
        /// <inheritdoc/>
        public virtual IProjectionFieldHandler Wrap(IProjectionFieldInterceptor interceptor)
        {
            if (interceptor is IProjectionFieldInterceptor<T> interceptorOfT)
            {
                return new ProjectionFieldWrapper<T>(this, interceptorOfT);
            }

            return this;
        }

        /// <inheritdoc/>
        public abstract bool CanHandle(ISelection selection);

        /// <inheritdoc/>
        public virtual T OnBeforeEnter(T context, ISelection selection) => context;

        /// <inheritdoc/>
        public abstract bool TryHandleEnter(
            T context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        /// <inheritdoc/>
        public virtual T OnAfterEnter(
            T context,
            ISelection selection,
            ISelectionVisitorAction action) => context;

        /// <inheritdoc/>
        public virtual T OnBeforeLeave(T context, ISelection selection) => context;

        /// <inheritdoc/>
        public abstract bool TryHandleLeave(
            T context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        /// <inheritdoc/>
        public virtual T OnAfterLeave(
            T context,
            ISelection selection,
            ISelectionVisitorAction action) => context;
    }
}
