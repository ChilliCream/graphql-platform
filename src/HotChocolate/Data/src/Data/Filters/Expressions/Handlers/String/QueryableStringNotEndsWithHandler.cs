using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotEndsWithHandler : QueryableStringOperationHandler
    {

        public QueryableStringNotEndsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotEndsWith;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.Not(
                FilterExpressionBuilder.EndsWith(property, parsedValue));
        }
    }
}
