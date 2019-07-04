using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class BooleanOperationHandler
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
            if (operation.Type == typeof(bool)
                && (value is BooleanValueNode || value.IsNull()))
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);

                object parserValue = type.ParseLiteral(value);

                switch (operation.Kind)
                {
                    case FilterOperationKind.Equals:
                        expression = FilterExpressionBuilder.Equals(
                            property, parserValue);
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = FilterExpressionBuilder.Not(
                            FilterExpressionBuilder.Equals(
                                property, parserValue)
                        );
                        return true;

                }
            }

            expression = null;
            return false;
        }

    }
}
