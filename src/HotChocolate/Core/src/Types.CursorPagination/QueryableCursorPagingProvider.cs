using System;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination
{
    public class QueryableCursorPagingProvider
        : CursorPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(QueryableCursorPagingProvider).GetMethod(
                nameof(CreateHandlerInternal), 
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public override bool CanHandle(IExtendedType source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.IsArrayOrList;
        }

        protected override CursorPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return (CursorPagingHandler)_createHandler
                .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
                .Invoke(null, new object[] { options })!;
        }

        private static QueryableCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
            PagingOptions options) =>
            new QueryableCursorPagingHandler<TEntity>(options);
    }
}
