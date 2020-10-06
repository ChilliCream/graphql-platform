using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public class QueryableProjectionListHandler
        : QueryableProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection)
        {
            return selection.Field.Type is ListType ||
                selection.Field.Type is NonNullType nonNullType &&
                nonNullType.InnerType() is ListType;
        }

        public override QueryableProjectionContext OnBeforeEnter(
            QueryableProjectionContext context,
            ISelection selection)
        {
            IObjectField field = selection.Field;
            Expression next;
            if (field.Member is PropertyInfo propertyInfo)
            {
                next = Expression.Property(context.GetInstance(), propertyInfo);
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                next = Expression.Call(context.GetInstance(), methodInfo);
            }
            else
            {
                throw new InvalidOperationException();
            }

            context.PushInstance(next);

            return context;
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

            IOutputType type = field.Type;

            Type clrType = type.IsListType()
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
            IObjectField field = selection.Field;

            if (field.RuntimeType is null ||
                field.Member is null)
            {
                action = null;
                return false;
            }

            ProjectionScope<Expression> scope = context.PopScope();

            if (!(scope is QueryableProjectionScope queryableScope) ||
                !context.TryGetQueryableScope(out QueryableProjectionScope? parentScope))
            {
                action = null;
                return false;
            }

            Type type;
            if (field.Member is PropertyInfo propertyInfo)
            {
                type = propertyInfo.PropertyType;
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                type = methodInfo.ReturnType;
            }
            else
            {
                throw new InvalidOperationException();
            }

            Expression select = queryableScope.CreateSelection(
                context.PopInstance(),
                type);

            parentScope.Level.Peek()
                .Enqueue(
                    Expression.Bind(field.Member, select));

            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
