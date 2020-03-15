using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.DotNetTypeInfoFactory;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultHandler : IListHandler
    {
        private const int TakeAmountForSingleOrDefault = 2;

        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            ObjectField field = context.FieldSelection.Field;
            if (field.ContextData.ContainsKey(nameof(SingleOrDefaultOptions)) &&
                field.Member is PropertyInfo propertyInfo)
            {
                var allowMultipleResults = field.ContextData[nameof(SingleOrDefaultOptions)]
                        is SingleOrDefaultOptions options && options.AllowMultipleResults;

                var takeAmount = allowMultipleResults ? 1 : TakeAmountForSingleOrDefault;

                Type elementType = GetInnerListType(propertyInfo.PropertyType);

                return Expression.Call(
                    typeof(Enumerable),
                    "Take",
                    new[] { elementType },
                    expression,
                    Expression.Constant(takeAmount));
            }

            return expression;
        }
    }
}
