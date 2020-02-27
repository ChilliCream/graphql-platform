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

            if (operation.Kind == SortOperationKind.Desc)
            {
                return source.OrderByDescending(lambda);
            }

            return source.OrderBy(lambda);
        }

        internal static IOrderedQueryable<TSource> AddSortOperation<TSource>(
            this IOrderedQueryable<TSource> source,
            SortOperationInvocation operation,
            ParameterExpression parameter)
        {
            Expression<Func<TSource, object>> lambda
                = HandleProperty<TSource>(operation, parameter);

            if (operation.Kind == SortOperationKind.Desc)
            {
                return source.ThenByDescending(lambda);
            }

            return source.ThenBy(lambda);
        }

        internal static Expression CompileInitialSortOperation(
            this Expression source,
            SortOperationInvocation operation,
            ParameterExpression parameter)
        {
            Expression lambda
                = HandleProperty(operation, parameter);

            if (operation.Kind == SortOperationKind.Desc)
            {
                return Expression.Call(
                    typeof(Enumerable),
                    "OrderByDescending",
                    new[] { operation.Property.DeclaringType, operation.Property.PropertyType },
                    source,
                    lambda);
            }

            return Expression.Call(
                typeof(Enumerable),
                "OrderBy",
                new[] { operation.Property.DeclaringType, operation.Property.PropertyType },
                source,
                lambda);
        }

        internal static Expression CompileSortOperation(
            this Expression source,
            SortOperationInvocation operation,
            ParameterExpression parameter)
        {
            Expression lambda
                = HandleProperty(operation, parameter);

            Type type = typeof(Enumerable);
            if (typeof(IOrderedQueryable).IsAssignableFrom(source.Type))
            {
                type = typeof(Queryable);
            }

            if (operation.Kind == SortOperationKind.Desc)
            {
                return Expression.Call(
                    type,
                    "ThenByDescending",
                    new[] { operation.Property.DeclaringType, operation.Property.PropertyType },
                    source,
                    lambda);
            }

            return Expression.Call(
                type,
                "ThenBy",
                new[] { operation.Property.DeclaringType, operation.Property.PropertyType },
                source,
                lambda);
        }

        internal static Expression<Func<TSource, object>> HandleProperty<TSource>(
            SortOperationInvocation operation, ParameterExpression parameter)
        {
            PropertyInfo propertyInfo = operation.Property;

            MemberExpression property = Expression.Property(parameter, propertyInfo);
            UnaryExpression propAsObject = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<TSource, object>>(propAsObject, parameter);
        }

        internal static Expression HandleProperty(
            SortOperationInvocation operation, ParameterExpression parameter)
        {
            PropertyInfo propertyInfo = operation.Property;

            MemberExpression property = Expression.Property(parameter, propertyInfo);
            return Expression.Lambda(property, parameter);
        }
    }
}
