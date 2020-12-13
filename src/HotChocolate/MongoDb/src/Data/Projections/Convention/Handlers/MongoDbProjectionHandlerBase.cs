using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// A handler that can intersect a <see cref="ISelection"/> and optimize the selection set for
    /// mongodb projections.
    /// </summary>
    public abstract class MongoDbProjectionHandlerBase
        : ProjectionFieldHandler<MongoDbProjectionVisitorContext>
    {
        /// <inheritdoc/>
        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        /// <inheritdoc/>
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
