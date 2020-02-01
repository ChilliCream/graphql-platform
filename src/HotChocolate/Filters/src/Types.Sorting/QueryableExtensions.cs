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
            SortOperationInvocation operation)
        {
            Expression<Func<TSource, object>> lambda
                = HandleProperty<TSource>(operation);

            if (operation.Kind == SortOperationKind.Desc)
            {
                return source.OrderByDescending(lambda);
            }

            return source.OrderBy(lambda);
        }

        internal static IOrderedQueryable<TSource> AddSortOperation<TSource>(
            this IOrderedQueryable<TSource> source,
            SortOperationInvocation operation)
        {
            Expression<Func<TSource, object>> lambda
                = HandleProperty<TSource>(operation);

            if (operation.Kind == SortOperationKind.Desc)
            {
                return source.ThenByDescending(lambda);
            }

            return source.ThenBy(lambda);
        }

        internal static Expression<Func<TSource, object>> HandleProperty<TSource>(
            SortOperationInvocation operation)
        {
            UnaryExpression propAsObject = Expression.Convert(
                operation.ExpressionBody,
                typeof(object)
            );
            return Expression.Lambda<Func<TSource, object>>(
                propAsObject,
                operation.Parameter
           );
        }
    }
}
