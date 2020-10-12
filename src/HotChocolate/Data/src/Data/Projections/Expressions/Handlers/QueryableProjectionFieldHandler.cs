using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public class QueryableProjectionFieldHandler
        : QueryableProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection) =>
            selection.Field.Member is {} &&
            selection.SelectionSet is not null;

        public override bool TryHandleEnter(
            QueryableProjectionContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;

            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            Expression nestedProperty;
            Type memberType;
            if (field.Member is PropertyInfo propertyInfo)
            {
                memberType = propertyInfo.PropertyType;
                nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                memberType = methodInfo.ReturnType;
                nestedProperty = Expression.Call(context.GetInstance(), methodInfo);
            }
            else
            {
                throw new InvalidOperationException();
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
            QueryableProjectionContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;

            if (field.RuntimeType is null ||
                field.Member is null)
            {
                action = null;
                return false;
            }

            // Deque last
            ProjectionScope<Expression> scope = context.PopScope();

            if (!(scope is QueryableProjectionScope queryableScope))
            {
                action = null;
                return false;
            }

            Queue<MemberAssignment> members = queryableScope.Level.Pop();
            MemberInitExpression memberInit = ProjectionExpressionBuilder.CreateMemberInit(
                queryableScope.RuntimeType,
                members);

            if (!context.TryGetQueryableScope(out QueryableProjectionScope? parentScope))
            {
                //TODO Exception, Invalid State
                throw new Exception();
            }

            parentScope.Level.Peek()
                .Enqueue(
                    Expression.Bind(field.Member, memberInit));

            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
