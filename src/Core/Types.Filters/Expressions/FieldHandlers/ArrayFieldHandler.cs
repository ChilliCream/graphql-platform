using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ArrayFieldHandler : IExpressionFieldHandler
    {
        public bool Enter(
            IQueryableFilterVisitorContext context,
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            out VisitorAction action
            )
        {
            if (field.Operation.Kind == FilterOperationKind.ArraySome 
                || field.Operation.Kind == FilterOperationKind.ArrayNone
                || field.Operation.Kind == FilterOperationKind.ArrayAll)
            {
                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    field.Operation.Property
                );

                context.PushInstance(nestedProperty);

                Type closureType = GetTypeFor(field.Operation);

                context.AddClosure(closureType);
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

            if (field.Operation.Kind == FilterOperationKind.ArraySome
                || field.Operation.Kind == FilterOperationKind.ArrayNone
                || field.Operation.Kind == FilterOperationKind.ArrayAll)
            {
                QueryableClosure nestedClosure = context.PopClosure();
                LambdaExpression lambda = nestedClosure.CreateLambda();
                Type closureType = GetTypeFor(field.Operation);

                Expression expression;
                switch (field.Operation.Kind)
                {
                    case FilterOperationKind.ArraySome:
                        expression = FilterExpressionBuilder.Any(
                          closureType,
                          context.GetInstance(),
                          lambda
                        );
                        break;
                        
                    case FilterOperationKind.ArrayNone:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Any(
                                closureType,
                                context.GetInstance(),
                                lambda
                            )
                        );
                        break;
                        
                    case FilterOperationKind.ArrayAll:
                        expression = FilterExpressionBuilder.All(
                          closureType,
                          context.GetInstance(),
                          lambda
                        );
                        break;
                        
                    default:
                        throw new NotSupportedException();
                }

                if (context.InMemory)
                {
                    expression = FilterExpressionBuilder.NotNullAndAlso(
                                 context.GetInstance(), expression);
                }

                context.GetLevel().Enqueue(expression);

                context.PopInstance();
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
