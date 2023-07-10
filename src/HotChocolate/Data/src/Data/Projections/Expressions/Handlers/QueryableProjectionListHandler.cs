using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.ExpressionUtils;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionListHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(ISelection selection) =>
        selection.Field.CanBeUsedInProjection() &&
        (selection.IsList ||
            selection.Field.ContextData.ContainsKey(SelectionOptions.MemberIsList));

    public override QueryableProjectionContext OnBeforeEnter(
        QueryableProjectionContext context,
        ISelection selection)
    {
        var field = selection.Field;
        var next = field.GetProjectionExpression(context.GetInstance());
        context.PushInstance(next);
        return context;
    }

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;
        var type = field.Type;

        var clrType = type.IsListType()
            ? type.ElementType().ToRuntimeType()
            : type.ToRuntimeType();

        // We add a new scope for the sub selection. This allows a new member initialization
        context.AddScope(clrType);

        action = SelectionVisitor.Continue;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var scope = context.PopScope();
        if (scope is not QueryableProjectionScope queryableScope ||
            !context.TryGetQueryableScope(out var parentScope))
        {
            action = null;
            return false;
        }

        // This can happen if the field handler below skips fields.
        bool hasProjectedFields = queryableScope.Level.TryPeek(out var q) && q.Count > 0;
        bool hasAbstractTypes = queryableScope.HasAbstractTypes();
        if (!hasAbstractTypes && !hasProjectedFields)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        var instance = context.PopInstance();
        var select = queryableScope.CreateSelection(instance);

        // Should this cast be left in?
        // select = select.MaybeCastValueTypeToObject();

        parentScope.Level.Peek().Enqueue(select);

        action = SelectionVisitor.Continue;
        return true;
    }
}
