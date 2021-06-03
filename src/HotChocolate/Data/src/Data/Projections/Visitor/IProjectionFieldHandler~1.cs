using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    /// <inheritdoc/>
    public interface IProjectionFieldHandler<TContext>
        : IProjectionFieldHandler
        where TContext : IProjectionVisitorContext
    {
        /// <summary>
        /// This method is called before the visitor calls
        /// <see cref="IProjectionFieldHandler{TContext}.TryHandleEnter"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        /// <returns>
        /// The instance of <see cref="TContext"/> that is used in TryHandleEnter
        /// </returns>
        TContext OnBeforeEnter(TContext context, ISelection selection);

        /// <summary>
        /// Tries to apply projection to the field. This method is called after
        /// <see cref="IProjectionFieldHandler{TContext}.OnBeforeEnter"/> and before
        /// <see cref="IProjectionFieldHandler{TContext}.OnAfterEnter"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        /// <param name="action">
        /// The <see cref="ISelectionVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleEnter(
            TContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        /// <summary>
        /// This method is called after the visitor calls
        /// <see cref="IProjectionFieldHandler{TContext}.TryHandleEnter"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        /// <param name="result">The action produced by TryHandleEnter</param>
        /// <returns>
        /// The instance of <see cref="TContext"/> that is used in on leave
        /// </returns>
        TContext OnAfterEnter(
            TContext context,
            ISelection selection,
            ISelectionVisitorAction result);

        /// <summary>
        /// This method is called before the visitor calls
        /// <see cref="IProjectionFieldHandler{TContext}.TryHandleLeave"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        /// <returns>
        /// The instance of <see cref="TContext"/> that is used in TryHandleLeave
        /// </returns>
        TContext OnBeforeLeave(TContext context, ISelection selection);

        /// <summary>
        /// Tries to apply projection to the field. This method is called after
        /// <see cref="IProjectionFieldHandler{TContext}.OnBeforeLeave"/> and before
        /// <see cref="IProjectionFieldHandler{TContext}.OnAfterLeave"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="selection">The current selection</param>
        /// <param name="action">
        /// The <see cref="ISelectionVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleLeave(
            TContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        /// <summary>
        /// This method is called after the visitor calls
        /// <see cref="IProjectionFieldHandler{TContext}.TryHandleLeave"/>
        /// </summary>
        /// <param name="context">The context of the <see cref="IProjectionVisitorContext"/></param>
        /// <param name="result">The action produced by TryHandleLeave</param>
        /// <param name="selection">The current selection</param>
        /// <returns>
        /// The instance of <see cref="TContext"/> that is used in TryHandleLeave
        /// </returns>
        TContext OnAfterLeave(
            TContext context,
            ISelection selection,
            ISelectionVisitorAction result);
    }
}
