using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
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
            out VisitorAction action
            )
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                var nestedProperty = Expression.Property(closures.Peek().Instance.Peek(), field.Operation.Property);
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
            Stack<QueryableClosure> closures)
        { 
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                // Deque last expression to prefix with nullcheck
                var condition = closures.Peek().Level.Peek().Dequeue();
                // wrap current property with null check
                var nullCheck = Expression.NotEqual(closures.Peek().Instance.Peek(), Expression.Constant(null, typeof(object)));
                // wrap last expression  
                closures.Peek().Level.Peek().Enqueue(Expression.AndAlso(nullCheck, condition));
                closures.Peek().Instance.Pop();
            }
        }

    }
}
