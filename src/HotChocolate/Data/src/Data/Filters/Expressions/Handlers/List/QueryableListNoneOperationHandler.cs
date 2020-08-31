using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableListNoneOperationHandler : QueryableListOperationHandlerBase
    {
        protected override int Operation => DefaultOperations.None;

        protected override Expression HandleListOperation(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            LambdaExpression lambda) =>
            FilterExpressionBuilder.Not(
                FilterExpressionBuilder.Any(closureType, context.GetInstance(), lambda));
    }
}
