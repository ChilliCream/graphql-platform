using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
{
    /// <inheritdoc/>
    public class MongoDbProjectionFieldHandler
        : MongoDbProjectionHandlerBase
    {
        /// <inheritdoc/>
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is not null;

        /// <inheritdoc/>
        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;
            context.Path.Push(field.GetName());
            action = SelectionVisitor.Continue;
            return true;
        }

        /// <inheritdoc/>
        public override bool TryHandleLeave(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            context.Path.Pop();

            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
