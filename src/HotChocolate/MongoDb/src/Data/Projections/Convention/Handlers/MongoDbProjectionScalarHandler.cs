using HotChocolate.Data.Projections;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
{
    /// <inheritdoc/>
    public class MongoDbProjectionScalarHandler
        : MongoDbProjectionHandlerBase
    {
        /// <inheritdoc/>
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is null;

        /// <inheritdoc/>
        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;
            context.Path.Push(field.GetName());
            context.Projections.Push(
                new MongoDbIncludeProjectionOperation(context.GetPath()));
            context.Path.Pop();

            action = SelectionVisitor.SkipAndLeave;
            return true;
        }
    }
}
