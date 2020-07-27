using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableStringInHandler : QueryableStringOperationHandler
    {
        protected override int Operation => Operations.In;

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            Expression property = context.GetInstance();
            if (context.TryGetParentField(out IFilterField? parentField))
            {
                return FilterExpressionBuilder.In(
                        property,
                        parentField.GetReturnType(),
                        parsedValue);
            }
            throw new InvalidOperationException();
        }
    }
}
