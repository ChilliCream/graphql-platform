using System.Linq.Expressions;
using HotChocolate.Language;

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
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.NotContains(property, parsedValue);
        }
    }
}
