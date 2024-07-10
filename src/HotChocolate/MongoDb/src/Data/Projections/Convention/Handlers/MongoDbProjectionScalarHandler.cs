using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.MongoDb.Projections;

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
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;
        context.Path.Push(field.GetName());
        context.Projections.Push(
            new MongoDbIncludeProjectionOperation(context.GetPath()));
        context.Path.Pop();

        action = SelectionVisitor.SkipAndLeave;
        return true;
    }
}
