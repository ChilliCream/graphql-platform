using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{

    public class QueryableStringEqualsHandler : QueryableStringOperationHandler
    {
        protected override int Operation => Operations.Equals;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType type,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.Equals(property, parsedValue);
        }
    }
}