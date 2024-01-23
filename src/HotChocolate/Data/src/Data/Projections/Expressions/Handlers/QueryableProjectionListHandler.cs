using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionListHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(ISelection selection) =>
        selection.Field.Member is { } &&
        (selection.IsList ||
            selection.Field.ContextData.ContainsKey(SelectionOptions.MemberIsList));

    public override QueryableProjectionContext OnBeforeEnter(
        QueryableProjectionContext context,
        ISelection selection)
    {
        var field = selection.Field;
        if (field.Member is PropertyInfo { CanWrite: true, })
        {
            var next = context.GetInstance().Append(field.Member);

            context.PushInstance(next);
        }

        return context;
    }

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        if (field.Member is not PropertyInfo { CanWrite: true, })
        {
            action = SelectionVisitor.Skip;

            return true;
        }

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
        var field = selection.Field;

        if (field.Member is null)
        {
            action = null;

            return false;
        }

        var scope = context.PopScope();

        if (!(scope is QueryableProjectionScope queryableScope) ||
            !context.TryGetQueryableScope(out var parentScope))
        {
            action = null;

            return false;
        }

        // in case the projection is empty we do not project. This can happen if the
        // field handler below skips fields
        if (!queryableScope.HasAbstractTypes() &&
            (queryableScope.Level.Count == 0 || queryableScope.Level.Peek().Count == 0))
        {
            action = SelectionVisitor.Continue;

            return true;
        }

        var type = field.Member.GetReturnType();

        var select = queryableScope.CreateSelection(context.PopInstance(), type);

        parentScope.Level.Peek().Enqueue(Expression.Bind(field.Member, select));

        action = SelectionVisitor.Continue;

        return true;
    }
}
