using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class StringOperationHandlers
    {
        private static Expression GetProperty(
            FilterOperation operation,
            IFilterVisitorContext<Expression> context)
        {
            Expression property = context.GetInstance();

            if (!operation.IsSimpleArrayType())
            {
                property = Expression.Property(context.GetInstance(), operation.Property);
            }
            return property;
        }
    }
}
