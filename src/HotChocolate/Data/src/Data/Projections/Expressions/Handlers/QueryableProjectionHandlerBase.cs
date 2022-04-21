using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public abstract class QueryableProjectionHandlerBase<TContext> : ProjectionFieldHandler<TContext>
    where TContext : QueryableProjectionContext
{
    public override bool TryHandleEnter(
        TContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        action = SelectionVisitor.Continue;
        return true;
    }

    public override bool TryHandleLeave(
        TContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        action = SelectionVisitor.Continue;
        return true;
    }
}
