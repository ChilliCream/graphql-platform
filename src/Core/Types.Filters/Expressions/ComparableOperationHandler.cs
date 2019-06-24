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
                        expression = FilterExpressionBuilder.Equals(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Equals(property, parsedValue)
                         );
                        return true;

                    case FilterOperationKind.GreaterThan:
                        expression = FilterExpressionBuilder.GreaterThan(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotGreaterThan:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.GreaterThan(property, parsedValue)
                         );
                        return true;


                    case FilterOperationKind.GreaterThanOrEqual:
                        expression = FilterExpressionBuilder.GreaterThanOrEqual(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotGreaterThanOrEqual:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.GreaterThanOrEqual(property, parsedValue)
                         );
                        return true;


                    case FilterOperationKind.LowerThan:
                        expression = FilterExpressionBuilder.LowerThan(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotLowerThan:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.LowerThan(property, parsedValue)
                         );
                        return true;


                    case FilterOperationKind.LowerThanOrEqual:
                        expression = FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue);
                        return true;

                    case FilterOperationKind.NotLowerThanOrEqual:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue)
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
                        expression = FilterExpressionBuilder.In(property, operation.Property.PropertyType, parsedValue);
                        return true;
                    case FilterOperationKind.NotIn:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.In(property, operation.Property.PropertyType, parsedValue)
                        );
                        return true;
                }
            }

            expression = null;
            return false;
        }


    }

}
