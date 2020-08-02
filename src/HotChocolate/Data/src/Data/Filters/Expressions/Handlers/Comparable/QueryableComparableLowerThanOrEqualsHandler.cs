using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableComparableLowerThanOrEqualsHandler
        : QueryableComparableOperationHandler
    {
        public QueryableComparableLowerThanOrEqualsHandler(
            ITypeConverter typeConverter)
            : base(typeConverter)
        {
        }

        protected override int Operation => DefaultOperations.LowerThanOrEquals;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            parsedValue = ParseValue(value, parsedValue, fieldType, context);
            return FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue);
        }
    }
}
