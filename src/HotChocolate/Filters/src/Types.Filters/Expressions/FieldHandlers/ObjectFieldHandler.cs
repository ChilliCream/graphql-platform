using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
    public class ObjectFieldHandler
        : IExpressionFieldHandler
    {
        public bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context,
            out ISyntaxVisitorAction action)
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    field.Operation.Property);
                context.PushInstance(nestedProperty);
                action = SyntaxVisitor.Continue;
                return true;
            }
            action = SyntaxVisitor.SkipAndLeave;
            return false;
        }

        public void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context)
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                // Deque last expression to prefix with nullcheck
                Expression condition = context.GetLevel().Dequeue();
                Expression property = context.GetInstance();

                // wrap last expression only if  in memory
                if (context.InMemory)
                {
                    condition = FilterExpressionBuilder.NotNullAndAlso(
                        property, condition);
                }

                context.GetLevel().Enqueue(condition);
                context.PopInstance();
            }
        }
    }
}
