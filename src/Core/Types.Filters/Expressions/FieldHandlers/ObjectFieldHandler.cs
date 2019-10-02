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
            Stack<Queue<Expression>> level,
            Stack<Expression> instance,
            out VisitorAction action
            )
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                instance.Push(Expression.Property(instance.Peek(), field.Operation.Property)); 
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
            Stack<Queue<Expression>> level,
            Stack<Expression> instance)
        {

            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                // Deque last expression to prefix with nullcheck
                var condition = level.Peek().Dequeue();
                // wrap current property with null check
                var nullCheck = Expression.NotEqual(instance.Peek(), Expression.Constant(null, typeof(object)));
                // wrap last expression  
                level.Peek().Enqueue(Expression.AndAlso(nullCheck, condition));
                instance.Pop();
            }
        }

    }
}
