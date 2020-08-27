using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringNotInHandler : QueryableStringOperationHandler
    {
        protected override int Operation => DefaultOperations.NotIn;

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
