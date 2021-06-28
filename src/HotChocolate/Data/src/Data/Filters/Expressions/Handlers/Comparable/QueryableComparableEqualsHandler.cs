using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableComparableEqualsHandler
        : QueryableComparableOperationHandler
    {
        public QueryableComparableEqualsHandler(
            ITypeConverter typeConverter)
            : base(typeConverter)
        {
        }

        protected override int Operation => DefaultFilterOperations.Equals;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            Expression property = context.GetInstance();
            parsedValue = ParseValue(value, parsedValue, field.Type, context);
            return FilterExpressionBuilder.Equals(property, parsedValue);
        }
    }
}
