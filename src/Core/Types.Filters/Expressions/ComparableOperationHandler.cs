using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ComparableOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IValueNode value,
            Expression instance,
            out Expression expression)
        {
            if (operation.Type == typeof(IComparable) && value is IValueNode<IComparable> s)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);

                switch (operation.Kind)
                {
                    case FilterOperationKind.Equals:
                        expression = Expression.Equal(
                            property,
                            Expression.Constant(s.Value));
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = Expression.Equal(
                            Expression.Equal(
                                property,
                                Expression.Constant(s.Value)),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.GreaterThan:
                        expression = Expression.GreaterThan(
                            Expression.Equal(
                                property,
                                Expression.Constant(s.Value)),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.GreaterThanOrEqual:
                        expression = Expression.GreaterThanOrEqual(
                            Expression.Equal(
                                property,
                                Expression.Constant(s.Value)),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.LowerThan:
                        expression = Expression.LessThan(
                            Expression.Equal(
                                property,
                                Expression.Constant(s.Value)),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.LowerThanOrEqual:
                        expression = Expression.LessThanOrEqual(
                            Expression.Equal(
                                property,
                                Expression.Constant(s.Value)),
                            Expression.Constant(false));
                        return true;

                }
            }

            expression = null;
            return false;
        }
    }
}
