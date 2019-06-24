using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class StringOperationHandler
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
            if (operation.Type == typeof(string) && value is StringValueNode s)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);

                switch (operation.Kind)
                {
                    case FilterOperationKind.Equals:
                        expression = FilterExpressionBuilder.Equals(
                            property, s.Value);
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Equals(property, s.Value)
                        );
                        return true;

                    case FilterOperationKind.StartsWith:
                        expression = FilterExpressionBuilder.StartsWith(
                            property, s.Value);
                        return true;

                    case FilterOperationKind.EndsWith:
                        expression = FilterExpressionBuilder.EndsWith(
                            property, s.Value);
                        return true;

                    case FilterOperationKind.NotStartsWith:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.StartsWith(
                                property, s.Value)
                        );
                        return true;

                    case FilterOperationKind.NotEndsWith:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.EndsWith(property, s.Value)
                        );
                        return true;

                    case FilterOperationKind.Contains:
                        expression = FilterExpressionBuilder.Contains(
                            property, s.Value);
                        return true;

                    case FilterOperationKind.NotContains:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Contains(property, s.Value)
                        );
                        return true;
                }
            }

            if (operation.Type == typeof(string) && value is ListValueNode li)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);
                var parsedValue = type.ParseLiteral(value);

                switch (operation.Kind)
                {
                    case FilterOperationKind.In:
                        expression = FilterExpressionBuilder.In(
                            property,
                            operation.Property.PropertyType,
                            parsedValue);
                        return true;

                    case FilterOperationKind.NotIn:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.In(
                                property,
                                operation.Property.PropertyType,
                                parsedValue));
                        return true;
                }
            }

            expression = null;
            return false;
        }
    }
}
