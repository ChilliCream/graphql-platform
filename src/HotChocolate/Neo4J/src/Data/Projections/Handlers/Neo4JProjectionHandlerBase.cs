using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <summary>
    /// A handler that can intersect a <see cref="ISelection"/> and optimize the selection set for
    /// Neo4J projections.
    /// </summary>
    public abstract class Neo4JProjectionHandlerBase
        : ProjectionFieldHandler<Neo4JProjectionVisitorContext>
    {
        /// <inheritdoc/>
        public override bool TryHandleEnter(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        /// <inheritdoc/>
        public override bool TryHandleLeave(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
