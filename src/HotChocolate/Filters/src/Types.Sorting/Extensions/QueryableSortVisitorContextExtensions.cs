using System;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting
{
    public static class QueryableSortVisitorContextExtensions
    {
        public static SortOperationInvocation CreateSortOperation(
            this QueryableSortVisitorContext context,
            SortOperationKind kind)
        {
            return context.Convention.OperationFactory(
                context.Convention,
                context,
                kind);
        }

        public static IQueryable<TSource> Sort<TSource>(
            this QueryableSortVisitorContext context,
            IQueryable<TSource> source)
        {
            if (context.SortOperations.Count == 0)
            {
                return source;
            }

            return source.Provider.CreateQuery<TSource>(context.Compile(source.Expression));
        }

        public static Expression Compile(
            this QueryableSortVisitorContext context,
            Expression source)
        {
            return context.Convention.Compiler(
                context.Convention,
                context,
                source);
        }
    }
}
