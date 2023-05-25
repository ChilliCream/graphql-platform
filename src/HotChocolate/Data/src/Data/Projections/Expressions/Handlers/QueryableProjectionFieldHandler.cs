using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using static HotChocolate.Data.Projections.Expressions.ProjectionExpressionBuilder;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionFieldHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(ISelection selection) =>
        selection.Field.CanBeUsedInProjection() &&
        selection.SelectionSet is not null;

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        var nestedProperty = field.GetProjectionExpression(context.GetInstance());
        var type = nestedProperty.Type;

        // We add a new scope for the sub selection. This allows a new member initialization
        context.AddScope(type);

        // We push the instance onto the new scope.
        // We do not need this instance on the current scope.
        context.PushInstance(nestedProperty);

        action = SelectionVisitor.Continue;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        // Dequeue last
        var scope = context.PopScope();

        if (scope is not QueryableProjectionScope queryableScope)
        {
            action = null;
            return false;
        }

        var memberInit = queryableScope.CreateMemberInit();

        if (!context.TryGetQueryableScope(out var parentScope))
        {
            throw ThrowHelper.ProjectionVisitor_InvalidState_NoParentScope();
        }

        var nestedProperty = scope.Instance.Peek();

        Expression rhs = memberInit;
        if (context.InMemory)
            rhs = NotNullAndAlso(nestedProperty, rhs, typeof(object[]));

        parentScope.Level
            .Peek()
            .Enqueue(rhs);

        action = SelectionVisitor.Continue;
        return true;
    }
}
