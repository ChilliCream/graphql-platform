using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ComparableOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            Expression instance,
            ITypeConversion converter,
            out Expression expression)
        {
            if (operation.Type == typeof(IComparable) && value is IValueNode<IComparable> s)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);
                var parsedValue = type.ParseLiteral(value);
                switch (operation.Kind)
                {
                    case FilterOperationKind.Equals:
                        expression = FilterExpressionBuilder.CreateEqualsExpression(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateEqualsExpression(property, parsedValue)
                         );
                        return true;

                    case FilterOperationKind.GreaterThan:
                        expression = FilterExpressionBuilder.CreateGreaterThanExpression(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotGreaterThan:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateGreaterThanExpression(property, parsedValue)
                         );
                        return true;


                    case FilterOperationKind.GreaterThanOrEqual:
                        expression = FilterExpressionBuilder.CreateGreaterThanOrEqualExpression(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotGreaterThanOrEqual:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateGreaterThanOrEqualExpression(property, parsedValue)
                         );
                        return true;


                    case FilterOperationKind.LowerThan:
                        expression = FilterExpressionBuilder.CreateLowerThanExpression(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotLowerThan:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateLowerThanExpression(property, parsedValue)
                         );
                        return true;


                    case FilterOperationKind.LowerThanOrEqual:
                        expression = FilterExpressionBuilder.CreateLowerThanOrEqualExpression(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotLowerThanOrEqual:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateLowerThanOrEqualExpression(property, parsedValue)
                         );
                        return true;


                }
            }

            if (operation.Type == typeof(IComparable) && value is ListValueNode li)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);
                var parsedValue = type.ParseLiteral(value);
                switch (operation.Kind)
                {
                    case FilterOperationKind.In:
                        expression = FilterExpressionBuilder.CreateInExpression(property, operation.Property.PropertyType, parsedValue);
                        return true;
                    case FilterOperationKind.NotIn:
                        expression = FilterExpressionBuilder.Not(FilterExpressionBuilder.CreateInExpression(property, operation.Property.PropertyType, parsedValue));
                        return true;
                }
            }

            expression = null;
            return false;
        }


    }

}
