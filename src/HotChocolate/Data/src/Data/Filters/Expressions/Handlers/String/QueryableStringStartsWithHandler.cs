using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringStartsWithHandler : QueryableStringOperationHandler
    {
        public QueryableStringStartsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.StartsWith;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.StartsWith(property, parsedValue);
        }
    }
}
