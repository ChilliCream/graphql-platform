using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableComparableGreaterThanOrEqualsHandler
        : QueryableComparableOperationHandler
    {
        public QueryableComparableGreaterThanOrEqualsHandler(
            ITypeConverter typeConverter)
            : base(typeConverter)
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.GreaterThanOrEquals;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            Expression property = context.GetInstance();
            parsedValue = ParseValue(value, parsedValue, field.Type, context);

            if (parsedValue is null)
            {
                throw new InvalidOperationException();
            }

            return FilterExpressionBuilder.GreaterThanOrEqual(
                property, parsedValue);
        }
    }
}
