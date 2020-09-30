using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldHandler<in TContext>
        : IProjectionFieldHandler
        where TContext : IProjectionVisitorContext
    {
        bool TryHandleEnter(
            TContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);

        bool TryHandleLeave(
            TContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action);
    }
}
