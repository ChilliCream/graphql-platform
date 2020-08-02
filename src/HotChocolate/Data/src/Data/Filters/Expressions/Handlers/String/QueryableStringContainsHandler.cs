using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringContainsHandler : QueryableStringOperationHandler
    {
        protected override int Operation => DefaultOperations.Contains;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.Contains(property, parsedValue);
        }
    }
}