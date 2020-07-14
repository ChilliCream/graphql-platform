using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    internal static class QueryableExtensions
    {
        internal static Expression CompileInitialSortOperation(
            this Expression source,
            SortOperationInvocation operation,
            ParameterExpression parameter)
        {
            Expression lambda
                = HandleProperty(operation, parameter);

            Type type = typeof(Enumerable);
            if (typeof(IOrderedQueryable).IsAssignableFrom(source.Type) ||
                typeof(IQueryable).IsAssignableFrom(source.Type))
            {
                type = typeof(Queryable);
            }

            if (operation.Kind == SortOperationKind.Desc)
            {
                return Expression.Call(
                    type,
                    "OrderByDescending",
                    new[] { parameter.Type, operation.Property.PropertyType },
                    source,
                    lambda);
            }

            return Expression.Call(
                type,
                "OrderBy",
                new[] { parameter.Type, operation.Property.PropertyType },
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
                    new[] { parameter.Type, operation.Property.PropertyType },
                    source,
                    lambda);
            }

            return Expression.Call(
                type,
                "ThenBy",
                new[] { parameter.Type, operation.Property.PropertyType },
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
