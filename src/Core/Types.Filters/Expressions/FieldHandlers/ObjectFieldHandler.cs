using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ObjectFieldHandler : IExpressionFieldHandler
    {
        public bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            Stack<QueryableClosure> closures,
            bool inMemory,
            out VisitorAction action)
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                MemberExpression nestedProperty = Expression.Property(
                    closures.Peek().Instance.Peek(),
                    field.Operation.Property);
                closures.Peek().Instance.Push(nestedProperty);
                action = VisitorAction.Continue;
                return true;
            }
            action = VisitorAction.Default;
            return false;
        }

        public void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            Stack<QueryableClosure> closures,
            bool inMemory)
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                // Deque last expression to prefix with nullcheck
                Expression condition = closures.Peek().Level.Peek().Dequeue();
                Expression property = closures.Peek().Instance.Peek();

                // wrap last expression only if  in memory
                if (inMemory)
                {
                    condition = FilterExpressionBuilder.NotNullAndAlso(
                        property, condition);
                }
                closures.Peek().Level.Peek().Enqueue(condition);


                closures.Peek().Instance.Pop();
            }
        }
    }
}
