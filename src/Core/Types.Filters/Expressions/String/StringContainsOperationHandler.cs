using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class StringContainsOperationHandler
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
            if (operation.Type == typeof(string)
                && (value is StringValueNode || value.IsNull()))
            {
                object parsedValue = type.ParseLiteral(value);

                MemberExpression property =
                    Expression.Property(instance, operation.Property);

                switch (operation.Kind)
                {
                    case FilterOperationKind.Contains:
                        expression = FilterExpressionBuilder.Contains(
                            property, parsedValue);
                        return true;

                    case FilterOperationKind.NotContains:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Contains(
                                property, parsedValue)
                        );
                        return true;
                }
            }

            expression = null;
            return false;
        }
    }
}
