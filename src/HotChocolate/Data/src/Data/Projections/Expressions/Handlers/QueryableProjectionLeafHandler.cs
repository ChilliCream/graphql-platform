using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public abstract class QueryableProjectionHandlerBase
        : IProjectionFieldHandler<QueryableProjectionContext>
    {
        public abstract bool CanHandle(ISelection selection);

        public virtual Selection RewriteSelection(Selection selection)
        {
            return selection;
        }

        public virtual bool TryHandleEnter(
            QueryableProjectionContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        public virtual bool TryHandleLeave(
            QueryableProjectionContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }
    }

    public class QueryableProjectionFieldHandler
        : QueryableProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection)
        {
            return selection.Field is not null;
        }
    }

    public class QueryableProjectionLeafHandler
        : QueryableProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection)
        {
            return selection.SelectionSet is null;
        }

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
            if (field.Member is PropertyInfo propertyInfo)
            {
                nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                nestedProperty = Expression.Call(context.GetInstance(), methodInfo);
            }
            else
            {
                throw new InvalidOperationException();
            }

            context.PushInstance(nestedProperty);
            context.AddScope();
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

            queryableScope.Level.Peek()
                .Enqueue(
                    Expression.Bind(field.Member, memberInit));
            context.PopInstance();

            action = SelectionVisitor.Continue;
            return true;
        }
    }

    public static class ProjectionVisitorContextExtensions
    {
        public static void ReportError<T>(
            this IProjectionVisitorContext<T> context,
            IError error) =>
            context.Errors.Add(error);

        public static ProjectionScope<T> AddScope<T>(
            this IProjectionVisitorContext<T> context)
        {
            ProjectionScope<T> closure = context.CreateScope();
            context.Scopes.Push(closure);
            return closure;
        }

        public static ProjectionScope<T> GetScope<T>(
            this IProjectionVisitorContext<T> context) =>
            context.Scopes.Peek();

        public static T GetInstance<T>(
            this IProjectionVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Peek();

        public static void PushInstance<T>(
            this IProjectionVisitorContext<T> context,
            T nextExpression) =>
            context.Scopes.Peek().Instance.Push(nextExpression);

        public static T PopInstance<T>(this IProjectionVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Pop();

        public static ProjectionScope<T> PopScope<T>(
            this IProjectionVisitorContext<T> context) =>
            context.Scopes.Pop();
    }
}
