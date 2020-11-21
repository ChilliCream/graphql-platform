using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableListAllOperationHandler : QueryableListOperationHandlerBase
    {
        protected override int Operation => DefaultFilterOperations.All;

        protected override Expression HandleListOperation(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            LambdaExpression lambda) =>
            FilterExpressionBuilder.All(closureType, context.GetInstance(), lambda);
    }
}
