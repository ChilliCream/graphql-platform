using System;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    internal static class QueryableExtensions
    {
        internal static Expression CompileInitialSortOperation(
           this Expression source,
           SortOperationInvocation operation)
        {
            Expression lambda
                = operation.CreateProperty();

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
                    nameof(Queryable.OrderByDescending),
                    new[] { operation.Parameter.Type, operation.ReturnType },
                    source,
                    lambda);
            }

            return Expression.Call(
                type,
                nameof(Queryable.OrderBy),
                new[] { operation.Parameter.Type, operation.ReturnType },
                source,
                lambda);
        }

        internal static Expression CompileSortOperation(
            this Expression source,
            SortOperationInvocation operation)
        {
            Expression lambda
                = operation.CreateProperty();

            Type type = typeof(Enumerable);
            if (typeof(IOrderedQueryable).IsAssignableFrom(source.Type))
            {
                type = typeof(Queryable);
            }

            if (operation.Kind == SortOperationKind.Desc)
            {
                return Expression.Call(
                    type,
                    nameof(Queryable.ThenByDescending),
                    new[] { operation.Parameter.Type, operation.ReturnType },
                    source,
                    lambda);
            }

            return Expression.Call(
                type,
                nameof(Queryable.ThenBy),
                new[] { operation.Parameter.Type, operation.ReturnType },
                source,
                lambda);
        }

        internal static Expression CreateProperty(
             this SortOperationInvocation operation)
        {
            return Expression.Lambda(
                operation.ExpressionBody,
                operation.Parameter
           );
        }
    }
}
