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
                        expression = Expression.Equal(
                            property,
                            Expression.Constant(parsedValue));
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = Expression.Equal(
                            Expression.Equal(
                                property,
                                Expression.Constant(parsedValue)),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.GreaterThan:
                        expression = Expression.GreaterThan(
                                property,
                                Expression.Constant(parsedValue));
                        return true;
                    
                    case FilterOperationKind.NotGreaterThan:
                        expression = Expression.Equal(
                            Expression.GreaterThan(
                                property,
                                Expression.Constant(parsedValue)),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.GreaterThanOrEqual:
                        expression = Expression.GreaterThanOrEqual(
                                property,
                                Expression.Constant(parsedValue));
                        return true;

                    case FilterOperationKind.NotGreaterThanOrEqual:
                        expression = Expression.Equal(
                           Expression.GreaterThanOrEqual(
                                property,
                                Expression.Constant(parsedValue)),
                        Expression.Constant(false));
                        return true;


                    case FilterOperationKind.LowerThan:
                        expression = Expression.LessThan(
                                property,
                                Expression.Constant(parsedValue));
                        return true;

                    case FilterOperationKind.NotLowerThan:
                        expression = Expression.Equal(
                           Expression.LessThan(
                                property,
                                Expression.Constant(parsedValue)),
                        Expression.Constant(false));
                        return true;

                    case FilterOperationKind.LowerThanOrEqual:
                        expression = Expression.LessThanOrEqual(
                                property,
                                Expression.Constant(parsedValue));
                        return true;

                    case FilterOperationKind.NotLowerThanOrEqual:
                        expression = Expression.Equal(
                           Expression.LessThanOrEqual(
                                property,
                                Expression.Constant(parsedValue)),
                        Expression.Constant(false));
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
                        expression = Expression.Call(
                            typeof(Enumerable),
                            "Contains",
                            new Type[] { operation.Property.PropertyType },
                            Expression.Constant(parsedValue),
                            property
                        );
                        return true;
                    case FilterOperationKind.NotIn:
                        expression = Expression.Equal(
                            Expression.Call(
                                typeof(Enumerable),
                                "Contains",
                                new Type[] { operation.Property.PropertyType },
                                Expression.Constant(parsedValue),
                            property
                            ),
                            Expression.Constant(false)
                        );
                        return true;
                }
            }

            expression = null;
            return false;
        }

    }

}
