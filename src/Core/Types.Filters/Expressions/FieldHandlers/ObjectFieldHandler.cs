using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ObjectFieldHandler : IExpressionFieldHandler
    {
        public bool Enter(
            IQueryableFilterVisitorContext context,
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            out VisitorAction action)
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    field.Operation.Property);
                context.PushInstance(nestedProperty);
                action = VisitorAction.Continue;
                return true;
            }
            action = VisitorAction.Default;
            return false;
        }

        public void Leave(
            IQueryableFilterVisitorContext context,
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
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
