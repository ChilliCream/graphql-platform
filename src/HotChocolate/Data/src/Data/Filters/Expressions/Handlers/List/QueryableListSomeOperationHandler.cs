using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableListSomeOperationHandler : QueryableListOperationHandlerBase
    {
        protected override int Operation => DefaultFilterOperations.Some;

        protected override Expression HandleListOperation(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            LambdaExpression lambda) =>
            FilterExpressionBuilder.Any(closureType, context.GetInstance(), lambda);
    }
}
