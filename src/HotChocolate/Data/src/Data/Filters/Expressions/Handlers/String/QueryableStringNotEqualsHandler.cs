using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotEqualsHandler : QueryableStringOperationHandler
    {
        protected override int Operation => Operations.NotEquals;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType type,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.NotEquals(property, parsedValue);
        }
    }
}
