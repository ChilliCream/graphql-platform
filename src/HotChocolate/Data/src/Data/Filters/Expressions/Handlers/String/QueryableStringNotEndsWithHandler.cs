using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotEndsWithHandler : QueryableStringOperationHandler
    {

        public QueryableStringNotEndsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultOperations.NotEndsWith;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            return FilterExpressionBuilder.Not(
                FilterExpressionBuilder.EndsWith(property, parsedValue));
        }
    }
}
