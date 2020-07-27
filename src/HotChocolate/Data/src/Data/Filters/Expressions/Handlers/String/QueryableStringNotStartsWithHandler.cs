using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotStartsWithHandler : QueryableStringOperationHandler
    {
        protected override int Operation => Operations.NotStartsWith;

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
                FilterExpressionBuilder.StartsWith(property, parsedValue));
        }
    }
}
