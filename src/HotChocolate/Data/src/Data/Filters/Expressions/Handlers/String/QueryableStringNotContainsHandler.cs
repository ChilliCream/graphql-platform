using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotContainsHandler : QueryableStringOperationHandler
    {
        public QueryableStringNotContainsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultOperations.NotContains;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.NotContains(property, parsedValue);
        }
    }
}
