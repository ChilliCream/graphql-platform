using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using static HotChocolate.Data.Projections.Expressions.ProjectionExpressionBuilder;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionObjectHandler<TContext> : QueryableProjectionHandlerBase<TContext>
    where TContext : QueryableProjectionContext
{
    public override bool CanHandle(ISelection selection) =>
        selection.Field.Member is { } &&
        selection.SelectionSet is not null;

    public override bool TryHandleEnter(
        TContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        IObjectField field = selection.Field;
        Expression nestedProperty;
        Type memberType;

        if (field.Member is PropertyInfo { CanWrite: true } propertyInfo)
        {
            memberType = propertyInfo.PropertyType;
            nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
        }
        else
        {
            action = SelectionVisitor.Skip;
            return true;
        }

        // We add a new scope for the sub selection. This allows a new member initialization
        context.AddScope(memberType);

        // We push the instance onto the new scope. We do not need this instance on the current
        // scope.
        context.PushInstance(nestedProperty);

        action = SelectionVisitor.Continue;
        return true;
    }

    public override bool TryHandleLeave(
        TContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        IObjectField field = selection.Field;

        if (field.Member is null)
        {
            action = null;
            return false;
        }

        // Deque last
        ProjectionScope<Expression> scope = context.PopScope();

        if (scope is not QueryableProjectionScope queryableScope)
        {
            action = null;
            return false;
        }

        Expression memberInit = queryableScope.CreateMemberInit();

        if (!context.TryGetQueryableScope(out QueryableProjectionScope? parentScope))
        {
            throw ThrowHelper.ProjectionVisitor_InvalidState_NoParentScope();
        }

        Expression nestedProperty;
        if (field.Member is PropertyInfo propertyInfo)
        {
            nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
        }
        else
        {
            action = SelectionVisitor.Skip;
            return true;
        }

        var memberBinding = ConstructMemberAssignment(context, field, nestedProperty, memberInit);

        parentScope.Level
            .Peek()
            .Enqueue(memberBinding);

        action = SelectionVisitor.Continue;
        return true;
    }

    protected virtual MemberAssignment ConstructMemberAssignment(TContext context, IObjectField field, Expression nestedProperty, Expression memberInit)
    {
        return Expression.Bind(field.Member!, NotNullAndAlso(nestedProperty, memberInit));
    }
}
