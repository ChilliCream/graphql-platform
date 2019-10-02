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
            Stack<QueryableClosure> closures,
            out VisitorAction action
            )
        {
            if (field.Operation.Kind == FilterOperationKind.ArraySome)
            {
                var nestedProperty = Expression.Property(
                    closures.Peek().Instance.Peek(),
                    field.Operation.Property
                );

                closures.Peek().Instance.Push(nestedProperty);

                closures.Push(new QueryableClosure(field.Operation.Type, "_s" + closures.Count));
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
            if (field.Operation.Kind == FilterOperationKind.ArraySome)
            {
                var nestedClosure = closures.Pop();
                var lambda = nestedClosure.CreateLambda();


                closures.Peek()
                    .Level.Peek()
                        .Enqueue(
                            FilterExpressionBuilder.Any(
                                field.Operation.Type,
                                closures.Peek().Instance.Peek(),
                                lambda
                         )
                     );

                closures.Peek().Instance.Pop();
            }
        }

    }
}
