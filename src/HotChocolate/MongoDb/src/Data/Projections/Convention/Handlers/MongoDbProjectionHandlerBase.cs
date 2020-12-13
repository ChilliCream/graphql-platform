using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.MongoDb
{
    public abstract class MongoDbProjectionHandlerBase
        : ProjectionFieldHandler<MongoDbProjectionVisitorContext>
    {
        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
