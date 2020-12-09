using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotInHandler : QueryableStringOperationHandler
    {
        protected override int Operation => DefaultFilterOperations.NotIn;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();

            return FilterExpressionBuilder.Not(
                FilterExpressionBuilder.In(
                    property,
                    context.RuntimeTypes.Peek().Source,
                    parsedValue));
        }
    }
}
