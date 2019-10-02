using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ArrayFieldHandler : IExpressionFieldHandler
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
            if (field.Operation.Kind == FilterOperationKind.ArraySome)
            {
                instance.Push(Expression.Property(instance.Peek(), field.Operation.Property));
                // testc = Expression.Parameter(field.Operation.Type, "s");
                //instance.Push(testc);
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
            if (field.Operation.Kind == FilterOperationKind.ArraySome)
            {
                instance.Pop();

                var anyMethod = typeof(Enumerable)
                    .GetMethods()
                    .Where(x => x.Name == "Any" && x.GetParameters().Length == 2)
                    .First()
                    .MakeGenericMethod(field.Operation.Type);
                var condition = level.Peek().Dequeue();
                var lambda = Expression.Lambda(condition, Expression.Parameter(field.Operation.Type, "s"));

                level.Peek().Enqueue(Expression.Call(anyMethod, new[] { instance.Peek(), lambda }));
                instance.Pop();
            }
        }

    }
}
