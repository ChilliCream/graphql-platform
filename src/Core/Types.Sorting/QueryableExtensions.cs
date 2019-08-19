using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    internal static class QueryableExtensions
    {
        internal static IOrderedQueryable<TSource> AddInitialSortOperation<TSource>(
            this IQueryable<TSource> source,
            SortOperationInvocation operation,
            ParameterExpression parameter)
        {
            Expression<Func<TSource, object>> lambda
                = HandleProperty<TSource>(operation, parameter);

            switch (operation.Kind)
            {
                case SortOperationKind.Desc:
                    return source.OrderByDescending(lambda);
                default:
                    return source.OrderBy(lambda);
            }
        }

        internal static IOrderedQueryable<TSource> AddSortOperation<TSource>(
            this IOrderedQueryable<TSource> source,
            SortOperationInvocation operation,
            ParameterExpression parameter)
        {
            Expression<Func<TSource, object>> lambda
                = HandleProperty<TSource>(operation, parameter);

            switch (operation.Kind)
            {
                case SortOperationKind.Desc:
                    return source.ThenByDescending(lambda);
                default:
                    return source.ThenBy(lambda);
            }
        }

        internal static Expression<Func<TSource, object>> HandleProperty<TSource>(
            SortOperationInvocation operation, ParameterExpression parameter)
        {
            PropertyInfo propertyInfo = operation.Property;

            MemberExpression property = Expression.Property(parameter, propertyInfo);
            UnaryExpression propAsObject = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<TSource, object>>(propAsObject, parameter);
        }
    }
}
