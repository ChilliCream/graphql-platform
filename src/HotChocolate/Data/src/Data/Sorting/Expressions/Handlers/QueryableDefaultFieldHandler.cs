using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableDefaultFieldHandler
        : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition) =>
            fieldDefinition.Member is not null;

        public override bool TryHandleEnter(
            QueryableSortContext context,
            ISortField field,
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

            if (!(context.GetInstance() is QueryableFieldSelector lastFieldSelector))
            {
                throw new InvalidOperationException();
            }

            Expression lastSelector = lastFieldSelector.Selector;


            Expression nextSelector;
            if (field.Member is PropertyInfo propertyInfo)
            {
                nextSelector = Expression.Property(lastSelector, propertyInfo);
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                nextSelector = Expression.Call(lastSelector, methodInfo);
            }
            else
            {
                throw new InvalidOperationException();
            }

            if (context.InMemory)
            {
                nextSelector = SortExpressionBuilder.IfNullThenDefault(
                    lastSelector,
                    nextSelector,
                    Expression.Default(field.RuntimeType.Source));
            }

            context.PushInstance(lastFieldSelector.WithSelector(nextSelector));
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            QueryableSortContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            // Deque last
            context.PopInstance();
            context.RuntimeTypes.Pop();

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}
