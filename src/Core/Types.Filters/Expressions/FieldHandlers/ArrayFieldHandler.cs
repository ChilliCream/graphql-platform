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
            if (
                field.Operation.Kind == FilterOperationKind.ArraySome ||
                field.Operation.Kind == FilterOperationKind.ArrayNone ||
                field.Operation.Kind == FilterOperationKind.ArrayAll
               )
            {
                var nestedProperty = Expression.Property(
                    closures.Peek().Instance.Peek(),
                    field.Operation.Property
                );

                closures.Peek().Instance.Push(nestedProperty);

                Type closureType = GetTypeFor(field.Operation);

                closures.Push(new QueryableClosure(closureType, "_s" + closures.Count));
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

            if (
               field.Operation.Kind == FilterOperationKind.ArraySome ||
               field.Operation.Kind == FilterOperationKind.ArrayNone ||
               field.Operation.Kind == FilterOperationKind.ArrayAll
              )
            {
                var nestedClosure = closures.Pop();
                var lambda = nestedClosure.CreateLambdaWithNullCheck();
                Type closureType = GetTypeFor(field.Operation);

                Expression expression;
                switch (field.Operation.Kind)
                {
                    case FilterOperationKind.ArraySome:
                        expression = FilterExpressionBuilder.Any(
                          closureType,
                          closures.Peek().Instance.Peek(),
                          lambda
                        );
                        break;
                    case FilterOperationKind.ArrayNone:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Any(
                                closureType,
                                closures.Peek().Instance.Peek(),
                                lambda
                            )
                        );
                        break;
                    case FilterOperationKind.ArrayAll:
                        expression = FilterExpressionBuilder.All(
                          closureType,
                          closures.Peek().Instance.Peek(),
                          lambda
                        );
                        break;
                    default:
                        throw new NotSupportedException();
                }
                closures.Peek().Level.Peek().Enqueue(expression);

                closures.Peek().Instance.Pop();
            }
        }

        private static Type GetTypeFor(FilterOperation operation)
        {
            if (operation.TryGetSimpleFilterBaseType(out Type baseType))
            {
                return baseType;
            }
            return operation.Type;
        }
    }
}
