using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringEndsWithHandler : QueryableStringOperationHandler
    {

        public QueryableStringEndsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultOperations.EndsWith;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.EndsWith(property, parsedValue);
        }
    }
}
