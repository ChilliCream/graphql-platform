using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableDefaultFieldHandler
        : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) =>
            !(fieldDefinition is FilterOperationFieldDefinition) &&
            fieldDefinition.Member is not null;

        public override bool TryHandleEnter(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

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
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            // Deque last
            Expression condition = context.GetLevel().Dequeue();

            context.PopInstance();
            context.RuntimeTypes.Pop();

            if (context.InMemory)
            {
                condition = FilterExpressionBuilder.NotNullAndAlso(
                    context.GetInstance(), condition);
            }

            context.GetLevel().Enqueue(condition);
            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}
