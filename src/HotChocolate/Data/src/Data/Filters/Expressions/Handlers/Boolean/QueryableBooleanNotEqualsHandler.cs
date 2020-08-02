using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableBooleanNotEqualsHandler
        : QueryableBooleanOperationHandler
    {
        protected override int Operation => DefaultOperations.NotEquals;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.NotEquals(property, parsedValue);
        }
    }
}
