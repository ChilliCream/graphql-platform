using HotChocolate.Data.Projections;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <inheritdoc/>
    public class Neo4JProjectionScalarHandler
        : Neo4JProjectionHandlerBase
    {
        /// <inheritdoc/>
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is null;

        /// <inheritdoc/>
        public override bool TryHandleEnter(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;
            context.Path.Push(field.GetName());
            context.Projections.Push(
                new Neo4JIncludeProjectionOperation(context.GetPath()));
            context.Path.Pop();

            action = SelectionVisitor.SkipAndLeave;
            return true;
        }
    }
}
