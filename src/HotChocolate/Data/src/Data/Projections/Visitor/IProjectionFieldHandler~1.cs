using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldHandler<TContext>
        : IProjectionFieldHandler
        where TContext : IProjectionVisitorContext
    {
        TContext OnBeforeEnter(TContext context, ISelection selection);

        bool TryHandleEnter(
            TContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        TContext OnAfterEnter(
            TContext context,
            ISelection selection,
            ISelectionVisitorAction result);

        TContext OnBeforeLeave(TContext context, ISelection selection);

        bool TryHandleLeave(
            TContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        TContext OnAfterLeave(
            TContext context,
            ISelection selection,
            ISelectionVisitorAction result);
    }
}
