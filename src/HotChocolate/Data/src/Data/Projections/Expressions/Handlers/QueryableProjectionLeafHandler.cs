using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public class QueryableProjectionLeafHandler
        : IProjectionFieldHandler<QueryableProjectionContext>
    {
        public bool CanHandle(ISelection selection)
        {
            return true;
        }

        public ISelection RewriteSelection(ISelection selection)
        {
            return selection;
        }

        public bool TryHandleEnter(
            QueryableProjectionContext context,
            ISelection selection,
            out ISelectionVisitorAction action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        public bool TryHandleLeave(
            QueryableProjectionContext context,
            ISelection selection,
            out ISelectionVisitorAction action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
