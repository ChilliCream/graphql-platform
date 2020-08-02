using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableComparableNotLowerThanHandler
        : QueryableComparableOperationHandler
    {
        public QueryableComparableNotLowerThanHandler(
            ITypeConverter typeConverter)
            : base(typeConverter)
        {
        }

        protected override int Operation => DefaultOperations.NotLowerThan;

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
            return FilterExpressionBuilder.Not(
                FilterExpressionBuilder.LowerThan(property, parsedValue));
        }
    }
}
