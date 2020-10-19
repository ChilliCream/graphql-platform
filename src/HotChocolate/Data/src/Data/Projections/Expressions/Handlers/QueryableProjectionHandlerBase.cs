using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public abstract class QueryableProjectionHandlerBase
        : ProjectionFieldHandler<QueryableProjectionContext>
    {
        public override bool TryHandleEnter(
            QueryableProjectionContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            QueryableProjectionContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
