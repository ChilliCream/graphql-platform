using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.MongoDb.Projections;

/// <inheritdoc/>
public class MongoDbProjectionFieldHandler
    : MongoDbProjectionHandlerBase
{
    /// <inheritdoc/>
    public override bool CanHandle(Selection selection)
        => !selection.IsLeaf;

    /// <inheritdoc/>
    public override bool TryHandleEnter(
        MongoDbProjectionVisitorContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;
        context.Path.Push(field.GetName());
        action = SelectionVisitor.Continue;
        return true;
    }

    /// <inheritdoc/>
    public override bool TryHandleLeave(
        MongoDbProjectionVisitorContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        context.Path.Pop();

        action = SelectionVisitor.Continue;
        return true;
    }

    public static MongoDbProjectionFieldHandler Create(ProjectionProviderContext context) => new();
}
