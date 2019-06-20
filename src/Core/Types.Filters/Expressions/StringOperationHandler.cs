using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
                        expression = FilterExpressionBuilder.CreateEqualExpression(property, s.Value);
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateEqualExpression(property, s.Value)
                        );
                        return true;

                    case FilterOperationKind.StartsWith:
                        expression = FilterExpressionBuilder.CreateStartsWithExpression(property, s.Value);
                        return true;

                    case FilterOperationKind.EndsWith:
                        expression = FilterExpressionBuilder.CreateEndsWithExpression(property, s.Value);
                        return true;

                    case FilterOperationKind.NotStartsWith:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateStartsWithExpression(property, s.Value)
                        );
                        return true;

                    case FilterOperationKind.NotEndsWith:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateEndsWithExpression(property, s.Value)
                        );
                        return true;

                    case FilterOperationKind.Contains:
                        expression = FilterExpressionBuilder.CreateContainsExpression(property, s.Value);
                        return true;

                    case FilterOperationKind.NotContains:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.CreateContainsExpression(property, s.Value)
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
