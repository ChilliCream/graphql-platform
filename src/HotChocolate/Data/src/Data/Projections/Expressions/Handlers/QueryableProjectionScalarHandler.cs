using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.ExpressionUtils;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionScalarHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(ISelection selection) =>
        selection.Field.CanBeUsedInProjection() &&
        selection.SelectionSet is null;

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        action = SelectionVisitor.SkipAndLeave;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        if (!context.TryGetQueryableScope(out var scope))
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        var instance = scope.Instance.Peek();
        var value = field.GetProjectionExpression(instance);

        scope.Level.Peek().Enqueue(value);

        action = SelectionVisitor.Continue;
        return true;
    }
}
