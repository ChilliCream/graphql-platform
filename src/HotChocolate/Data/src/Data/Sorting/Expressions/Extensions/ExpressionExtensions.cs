using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions;

public static class ExpressionExtensions
{
    public static Type GetEnumerableKind(this Expression source)
    {
        var type = typeof(Enumerable);
        if (typeof(IOrderedQueryable).IsAssignableFrom(source.Type) ||
            typeof(IQueryable).IsAssignableFrom(source.Type))
        {
            type = typeof(Queryable);
        }

        return type;
    }
}
